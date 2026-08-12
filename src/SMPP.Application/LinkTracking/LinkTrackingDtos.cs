namespace SMPP.Application.LinkTracking;

public record LinkSummaryDto(
    string Token,
    string DestinationUrl,
    int ClickCount,
    DateTime? FirstClickedAt,
    DateTime? LastClickedAt);

public record BatchLinkStatsDto(string BatchId, int TotalClicks, IReadOnlyList<LinkSummaryDto> Links);

public record LinkClickRowDto(DateTime ClickedAt, string IpAddress, string? UserAgent, string Token);
