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

/// <summary>
/// Abstraction over the external, out-of-repo gateway (legacy env var SMPP_API_URL). It offers
/// only the message preflight filter: actual delivery is not an API call at all, it happens by
/// handing a batch to the SMPP daemon through the under_process table.
/// </summary>
public interface ISmsGatewayClient
{
    Task<SpamFilterResult> PreflightFilterAsync(SpamFilterRequest request, CancellationToken ct = default);
}
