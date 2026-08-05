using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.SpamKeywords;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class SpamKeywordService : ISpamKeywordService
{
    private readonly SmppDbContext _db;

    public SpamKeywordService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SpamKeywordListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.SpamKeywords
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new SpamKeywordListItemDto(k.Id, k.Keyword, k.KeywordType, k.IsEnabled, k.CreatedAt))
            .ToListAsync(ct);

    public async Task<PagedResult<SpamKeywordListItemDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.SpamKeywords.AsNoTracking().OrderByDescending(k => k.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new SpamKeywordListItemDto(k.Id, k.Keyword, k.KeywordType, k.IsEnabled, k.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<SpamKeywordListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<int> CreateAsync(int createdByUserId, CreateSpamKeywordRequest request, CancellationToken ct = default)
    {
        var keyword = new SpamKeyword
        {
            Keyword = request.Keyword,
            KeywordType = request.KeywordType,
            CreatedByUserId = createdByUserId,
        };
        _db.SpamKeywords.Add(keyword);
        await _db.SaveChangesAsync(ct);
        return keyword.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var keyword = await _db.SpamKeywords.FindAsync(new object[] { id }, ct);
        if (keyword is not null)
        {
            _db.SpamKeywords.Remove(keyword);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task SetEnabledAsync(int id, bool isEnabled, CancellationToken ct = default)
    {
        var keyword = await _db.SpamKeywords.FindAsync(new object[] { id }, ct);
        if (keyword is not null)
        {
            keyword.IsEnabled = isEnabled;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<PagedResult<SpamBlockedAttemptRowDto>> GetBlockedAttemptsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.SpamBlockedAttempts.AsNoTracking().OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var usernames = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName!, ct);

        var items = rows
            .Select(r => new SpamBlockedAttemptRowDto(
                r.Id,
                r.CreatedAt,
                usernames.GetValueOrDefault(r.UserId, r.UserId.ToString()),
                r.Source,
                r.SenderId,
                r.RecipientCount,
                r.MatchedTerms,
                r.Message))
            .ToList();

        return new PagedResult<SpamBlockedAttemptRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }
}
