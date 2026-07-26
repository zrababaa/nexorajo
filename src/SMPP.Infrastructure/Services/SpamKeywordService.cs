using Microsoft.EntityFrameworkCore;
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
            .Select(k => new SpamKeywordListItemDto(k.Id, k.Keyword, k.KeywordType, k.CreatedAt))
            .ToListAsync(ct);

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
}
