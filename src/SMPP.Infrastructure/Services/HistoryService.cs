using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class HistoryService : IHistoryService
{
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
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var query = _db.Histories.Where(h => visibleUserIds.Contains(h.CreatedByUserId));
        if (source.HasValue)
        {
            query = query.Where(h => h.Source == source.Value);
        }
        if (!string.IsNullOrEmpty(campaignBatchId))
        {
            query = query.Where(h => h.CampaignBatchId == campaignBatchId);
        }
        query = query.OrderByDescending(h => h.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HistoryListItemDto(h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber, h.Status, h.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<HistoryListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<HistorySummaryDto> GetSummaryAsync(int currentUserId, UserRole role, string campaignBatchId, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var rows = await _db.Histories
            .Where(h => visibleUserIds.Contains(h.CreatedByUserId) && h.CampaignBatchId == campaignBatchId)
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
}
