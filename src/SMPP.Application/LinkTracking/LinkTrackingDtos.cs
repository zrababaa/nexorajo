namespace SMPP.Application.LinkTracking;

public record LinkSummaryDto(
    string Token,
    string DestinationUrl,
    int ClickCount,
    DateTime? FirstClickedAt,
    DateTime? LastClickedAt);

public record BatchLinkStatsDto(string BatchId, int TotalClicks, IReadOnlyList<LinkSummaryDto> Links);

public record LinkClickRowDto(DateTime ClickedAt, string IpAddress, string? UserAgent, string Token);

/// <summary>
/// One tracked link in the standalone "Tracking Links" listing: the rewritten short URL, its
/// real destination, the batch it went out with, and its running click totals. OwnerUsername is
/// populated for a Superadmin viewer (who sees every account's links) and null for an Account
/// viewer (who only ever sees its own).
/// </summary>
public record TrackedLinkRowDto(
    string Token,
    string ShortUrl,
    string DestinationUrl,
    string BatchId,
    int ClickCount,
    DateTime? FirstClickedAt,
    DateTime? LastClickedAt,
    DateTime CreatedAt,
    string? OwnerUsername);
