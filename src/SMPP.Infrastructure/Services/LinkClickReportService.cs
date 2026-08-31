using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    private readonly LinkTrackingOptions _options;

    public LinkClickReportService(SmppDbContext db, IUserScopeResolver scopeResolver, IOptions<LinkTrackingOptions> options)
    {
        _db = db;
        _scopeResolver = scopeResolver;
        _options = options.Value;
    }

    public async Task<PagedResult<TrackedLinkRowDto>> GetLinksAsync(
        int currentUserId, UserRole role, int page, int pageSize, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);
        var includeOwner = role == UserRole.Superadmin;

        var query = _db.TrackedLinks
            .AsNoTracking()
            // t.BatchId == "" is a link minted by "Insert link" that no send has claimed yet -
            // it isn't a link "found in a sent message", so it stays off this list until sent.
            .Where(t => visibleUserIds.Contains(t.CreatedByUserId) && t.BatchId != "")
            .OrderByDescending(t => t.Id);

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Token,
                t.DestinationUrl,
                t.ShortUrl,
                t.BatchId,
                t.ClickCount,
                t.FirstClickedAt,
                t.LastClickedAt,
                t.CreatedAt,
                OwnerUsername = includeOwner
                    ? _db.Users.Where(u => u.Id == t.CreatedByUserId).Select(u => u.UserName).FirstOrDefault()
                    : null,
            })
            .ToListAsync(ct);

        var baseUrl = (_options.BaseUrl ?? string.Empty).TrimEnd('/');
        var items = rows
            .Select(r => new TrackedLinkRowDto(
                r.Token,
                // The Short.io link when the send shortened it, otherwise the full /l/{token} form.
                r.ShortUrl ?? (baseUrl.Length == 0 ? $"/l/{r.Token}" : $"{baseUrl}/l/{r.Token}"),
                r.DestinationUrl,
                r.BatchId,
                r.ClickCount,
                r.FirstClickedAt,
                r.LastClickedAt,
                r.CreatedAt,
                r.OwnerUsername))
            .ToList();

        return new PagedResult<TrackedLinkRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
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
