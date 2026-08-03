using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Reads the delivery log the external SMPP daemon writes. That log lives in two tables, not
/// one: bulk and API batches land in <c>historys</c>, Quick Send batches in
/// <c>quick_send_history</c> (see <see cref="QuickSendHistory"/>). Both are read and the results
/// merged, which is why paging finishes in memory rather than in SQL - a page is a few dozen
/// rows, and no set operation spans two differently-shaped legacy tables.
/// </summary>
public class HistoryService : IHistoryService
{
    private const int MaxExportRows = 50_000;

    private readonly SmppDbContext _db;
    private readonly IUserScopeResolver _scopeResolver;
    private readonly QuickSendHistoryReader _quickSendReader;

    public HistoryService(SmppDbContext db, IUserScopeResolver scopeResolver, QuickSendHistoryReader quickSendReader)
    {
        _db = db;
        _scopeResolver = scopeResolver;
        _quickSendReader = quickSendReader;
    }

    public Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, MessageSource? source, string? campaignBatchId, int page, int pageSize, CancellationToken ct = default)
    {
        return GetPagedAsync(currentUserId, role, new HistoryFilterDto(source, CampaignBatchId: campaignBatchId), page, pageSize, ct);
    }

    public async Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        // Neither table can contribute more than one page's worth to the merged page, so each
        // side only needs the newest page*pageSize rows before the merge decides the order.
        var take = page * pageSize;

        var bulk = await BuildHistoryQuery(visibleUserIds, filter)
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .Select(h => new HistoryListItemDto(
                h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber, h.Status, h.CreatedAt, h.CreatedByUserId))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(visibleUserIds, filter, new List<HistoryListItemDto>(), query => query
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .Select(h => new HistoryListItemDto(
                h.Id, h.CampaignBatchId, MessageSource.QuickSend, h.SenderNumber, h.ReceiverNumber, h.Status, h.CreatedAt, h.CreatedByUserId))
            .ToListAsync(ct));

        var totalCount = await BuildHistoryQuery(visibleUserIds, filter).CountAsync(ct)
            + await ReadQuickSendAsync(visibleUserIds, filter, 0, query => query.CountAsync(ct));

        var items = bulk
            .Concat(quick)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<HistoryListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<HistorySummaryDto> GetSummaryAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var bulk = await BuildHistoryQuery(visibleUserIds, filter)
            .GroupBy(h => h.Status)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(visibleUserIds, filter, new List<StatusCount>(), query => query
            .GroupBy(h => h.Status)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToListAsync(ct));

        var counts = bulk
            .Concat(quick)
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

        int CountOf(MessageStatus status) => counts.GetValueOrDefault(status);

        return new HistorySummaryDto(
            Total: counts.Values.Sum(),
            Delivered: CountOf(MessageStatus.Delivered),
            Sent: CountOf(MessageStatus.Sent),
            Processing: CountOf(MessageStatus.Processing),
            Undelivered: CountOf(MessageStatus.Undelivered),
            Failed: CountOf(MessageStatus.Failed),
            Expired: CountOf(MessageStatus.Expired));
    }

    public async Task<IReadOnlyList<HistoryExportRowDto>> GetForExportAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var bulk = await BuildHistoryQuery(visibleUserIds, filter)
            .OrderByDescending(h => h.CreatedAt)
            .Take(MaxExportRows)
            .Select(h => new HistoryExportRowDto(
                h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber,
                h.MessageText, h.Status, h.ExternalMessageId, h.CreatedAt))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(visibleUserIds, filter, new List<HistoryExportRowDto>(), query => query
            .OrderByDescending(h => h.CreatedAt)
            .Take(MaxExportRows)
            .Select(h => new HistoryExportRowDto(
                h.Id, h.CampaignBatchId, MessageSource.QuickSend, h.SenderNumber, h.ReceiverNumber,
                h.MessageText, h.Status, h.ExternalMessageId, h.CreatedAt))
            .ToListAsync(ct));

        return bulk
            .Concat(quick)
            .OrderByDescending(h => h.CreatedAt)
            .Take(MaxExportRows)
            .ToList();
    }

    private IQueryable<History> BuildHistoryQuery(IReadOnlyCollection<int> visibleUserIds, HistoryFilterDto filter)
    {
        var query = _db.Histories.AsNoTracking().Where(h => visibleUserIds.Contains(h.CreatedByUserId));

        if (filter.Source.HasValue)
        {
            query = query.Where(h => h.Source == filter.Source.Value);
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(h => h.Status == filter.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.CampaignBatchId))
        {
            query = query.Where(h => h.CampaignBatchId == filter.CampaignBatchId);
        }
        if (!string.IsNullOrWhiteSpace(filter.ReceiverSearch))
        {
            query = query.Where(h => h.ReceiverNumber.Contains(filter.ReceiverSearch));
        }
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(h => h.CreatedAt >= fromUtc);
        }
        if (filter.DateTo.HasValue)
        {
            var toUtcExclusive = filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(h => h.CreatedAt < toUtcExclusive);
        }

        return query;
    }

    /// <summary>
    /// The same filters over <c>quick_send_history</c>. Deliberately a second copy rather than
    /// something generic over both entities: they are two independent legacy tables that only
    /// happen to look alike, and the shared version costs more in machinery than it saves.
    /// </summary>
    private static IQueryable<QuickSendHistory> ApplyQuickSendFilters(
        IQueryable<QuickSendHistory> source, IReadOnlyCollection<int> visibleUserIds, HistoryFilterDto filter)
    {
        var query = source.Where(h => visibleUserIds.Contains(h.CreatedByUserId));

        if (filter.Status.HasValue)
        {
            query = query.Where(h => h.Status == filter.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.CampaignBatchId))
        {
            query = query.Where(h => h.CampaignBatchId == filter.CampaignBatchId);
        }
        if (!string.IsNullOrWhiteSpace(filter.ReceiverSearch))
        {
            query = query.Where(h => h.ReceiverNumber.Contains(filter.ReceiverSearch));
        }
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(h => h.CreatedAt >= fromUtc);
        }
        if (filter.DateTo.HasValue)
        {
            var toUtcExclusive = filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(h => h.CreatedAt < toUtcExclusive);
        }

        return query;
    }

    /// <summary>
    /// Runs <paramref name="read"/> over the filtered <c>quick_send_history</c> rows, short of
    /// asking at all when the filter has already ruled Quick Send out.
    /// </summary>
    private Task<T> ReadQuickSendAsync<T>(
        IReadOnlyCollection<int> visibleUserIds, HistoryFilterDto filter, T empty, Func<IQueryable<QuickSendHistory>, Task<T>> read)
    {
        if (filter.Source.HasValue && filter.Source.Value != MessageSource.QuickSend)
        {
            return Task.FromResult(empty);
        }

        return _quickSendReader.ReadAsync(empty, query => read(ApplyQuickSendFilters(query, visibleUserIds, filter)));
    }

    private record StatusCount(MessageStatus Status, int Count);
}
