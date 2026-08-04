using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Matches the message against the keyword lists in-process - see <see cref="SpamKeywordMatcher"/>
/// for the rules themselves.
///
/// Legacy delegated this to the external gateway's <c>ulr-filter</c> endpoint and treated any
/// element it returned - from either keyword list - as a block. That host is frequently
/// unreachable from the app server, and the call carried a 30 second timeout that then failed
/// open, so in practice no message was ever filtered and every send stalled for half a minute
/// first. The matching is a substring scan over a handful of rows, so it runs here instead.
///
/// Both keyword lists block, exactly as legacy behaved: the Include/Exclude split is how the
/// operator categorises the words, not two different verdicts.
/// </summary>
public class SpamKeywordFilterService : ISpamKeywordFilterService
{
    private readonly SmppDbContext _db;

    public SpamKeywordFilterService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<SpamFilterResult> CheckAsync(string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new SpamFilterResult(false, Array.Empty<string>(), Array.Empty<string>());
        }

        var keywords = await _db.SpamKeywords
            .AsNoTracking()
            .Where(k => k.IsEnabled)
            .Select(k => new { k.Keyword, k.KeywordType })
            .ToListAsync(ct);

        var matchedKeywords = keywords
            .Where(k => k.KeywordType != SpamKeywordType.Url && SpamKeywordMatcher.ContainsKeyword(message, k.Keyword))
            .Select(k => k.Keyword)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedUrls = keywords
            .Where(k => k.KeywordType == SpamKeywordType.Url && SpamKeywordMatcher.ContainsUrl(message, k.Keyword))
            .Select(k => k.Keyword)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SpamFilterResult(
            IsBlocked: matchedKeywords.Count > 0 || matchedUrls.Count > 0,
            MatchedKeywords: matchedKeywords,
            MatchedUrls: matchedUrls);
    }
}
