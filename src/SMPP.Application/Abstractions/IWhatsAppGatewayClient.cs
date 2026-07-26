namespace SMPP.Application.Abstractions;

public record SpamFilterRequest(
    string Message,
    IReadOnlyCollection<string> IncludeKeywords,
    IReadOnlyCollection<string> ExcludeKeywords,
    IReadOnlyCollection<string> BlockedUrls);

public record SpamFilterResult(
    bool IsBlocked,
    IReadOnlyCollection<string> MatchedKeywords,
    IReadOnlyCollection<string> MatchedUrls);

public record GatewaySendResult(
    bool Success,
    string? ExternalMessageId,
    string RawResponse);

/// <summary>
/// Abstraction over the external, out-of-repo WhatsApp gateway (legacy env var
/// SMPP_API_URL). Kept as a thin, swappable/mockable HTTP client — the gateway's
/// wire contract is owned by an external system, not this app.
/// </summary>
public interface IWhatsAppGatewayClient
{
    Task<SpamFilterResult> PreflightFilterAsync(SpamFilterRequest request, CancellationToken ct = default);

    Task<GatewaySendResult> SendAsync(string message, string mobileNumber, string senderId, CancellationToken ct = default);
}
