using SMPP.Domain.Enums;

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

    /// <summary>
    /// Records a send rejected by <see cref="CheckAsync"/> so a Superadmin can review it under
    /// Content Filter &gt; Blocked Attempts. Nothing is charged or queued for a blocked send, so
    /// this is the only trace of it left anywhere.
    /// </summary>
    Task LogBlockedAttemptAsync(
        int userId,
        MessageSource source,
        string senderId,
        int recipientCount,
        IReadOnlyCollection<string> matchedTerms,
        string message,
        CancellationToken ct = default);
}
