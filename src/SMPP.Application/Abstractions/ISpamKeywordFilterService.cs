namespace SMPP.Application.Abstractions;

public record SpamFilterResult(
    bool IsBlocked,
    IReadOnlyCollection<string> MatchedKeywords,
    IReadOnlyCollection<string> MatchedUrls);

/// <summary>
/// Matches a message against the configured Include/Exclude/Url spam keyword lists. Used
/// uniformly by Quick Send, Bulk Send, and the public API - every send goes through it.
/// </summary>
public interface ISpamKeywordFilterService
{
    Task<SpamFilterResult> CheckAsync(string message, CancellationToken ct = default);
}
