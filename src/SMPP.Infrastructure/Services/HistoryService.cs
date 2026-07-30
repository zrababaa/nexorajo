using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private const int MaxExportRows = 50_000;

    private readonly SmppDbContext _db;
    private readonly IUserScopeResolver _scopeResolver;

    public HistoryService(SmppDbContext db, IUserScopeResolver scopeResolver)
    {
        _db = db;
        _scopeResolver = scopeResolver;
    }

    public async Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, MessageSource? source, string? campaignBatchId, int page, int pageSize, CancellationToken ct = default)
    {
        return await GetPagedAsync(currentUserId, role, new HistoryFilterDto(source, CampaignBatchId: campaignBatchId), page, pageSize, ct);
    }

    public async Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = await BuildFilteredQueryAsync(currentUserId, role, filter, ct);
        query = query.OrderByDescending(h => h.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HistoryListItemDto(h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber, h.Status, h.CreatedAt, h.CreatedByUserId))
            .ToListAsync(ct);

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
        var query = await BuildFilteredQueryAsync(currentUserId, role, filter, ct);

        var rows = await query
            .GroupBy(h => h.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountOf(MessageStatus status) => rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

        return new HistorySummaryDto(
            Total: rows.Sum(r => r.Count),
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
        var query = await BuildFilteredQueryAsync(currentUserId, role, filter, ct);

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .Take(MaxExportRows)
            .Select(h => new HistoryExportRowDto(
                h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber,
                h.MessageText, h.Status, h.ExternalMessageId, h.CreatedAt))
            .ToListAsync(ct);
    }

    private async Task<IQueryable<History>> BuildFilteredQueryAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, CancellationToken ct)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var query = _db.Histories.Where(h => visibleUserIds.Contains(h.CreatedByUserId));

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
}
