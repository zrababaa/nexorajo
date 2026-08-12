using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.LinkTracking;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class LinkClickReportService : ILinkClickReportService
{
    private readonly SmppDbContext _db;
    private readonly IUserScopeResolver _scopeResolver;

    public LinkClickReportService(SmppDbContext db, IUserScopeResolver scopeResolver)
    {
        _db = db;
        _scopeResolver = scopeResolver;
    }

    public async Task<BatchLinkStatsDto?> GetBatchStatsAsync(string batchId, int currentUserId, UserRole role, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var links = await _db.TrackedLinks
            .AsNoTracking()
            .Where(t => t.BatchId == batchId && visibleUserIds.Contains(t.CreatedByUserId))
            .OrderBy(t => t.Id)
            .Select(t => new LinkSummaryDto(t.Token, t.DestinationUrl, t.ClickCount, t.FirstClickedAt, t.LastClickedAt))
            .ToListAsync(ct);

        return links.Count == 0 ? null : new BatchLinkStatsDto(batchId, links.Sum(l => l.ClickCount), links);
    }

    public async Task<PagedResult<LinkClickRowDto>?> GetBatchClicksAsync(
        string batchId, int currentUserId, UserRole role, int page, int pageSize, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var visible = await _db.TrackedLinks
            .AsNoTracking()
            .AnyAsync(t => t.BatchId == batchId && visibleUserIds.Contains(t.CreatedByUserId), ct);

        if (!visible)
        {
            return null;
        }

        var query = _db.LinkClicks
            .AsNoTracking()
            .Where(c => c.BatchId == batchId)
            .Join(_db.TrackedLinks.AsNoTracking(), c => c.TrackedLinkId, t => t.Id,
                (c, t) => new { c.ClickedAt, c.IpAddress, c.UserAgent, t.Token })
            .OrderByDescending(x => x.ClickedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LinkClickRowDto(x.ClickedAt, x.IpAddress, x.UserAgent, x.Token))
            .ToListAsync(ct);

        return new PagedResult<LinkClickRowDto> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }
}
