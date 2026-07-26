using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.WhatsAppGateway;

/// <summary>
/// Thin HTTP client over the external, out-of-repo WhatsApp gateway. This app never delivers
/// messages itself - it only calls this gateway's preflight filter and send endpoints, and the
/// OutboundMessageWorker is the only caller. Kept behind IWhatsAppGatewayClient so the gateway
/// can be swapped/mocked without touching any send flow.
/// </summary>
public class WhatsAppGatewayClient : IWhatsAppGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppGatewayClient> _logger;

    public WhatsAppGatewayClient(HttpClient httpClient, IOptions<GatewayOptions> options, ILogger<WhatsAppGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    }

    public async Task<SpamFilterResult> PreflightFilterAsync(SpamFilterRequest request, CancellationToken ct = default)
    {
        try
        {
            var query = new Dictionary<string, string?>
            {
                ["message"] = request.Message,
                ["include"] = string.Join(",", request.IncludeKeywords),
                ["exclude"] = string.Join(",", request.ExcludeKeywords),
                ["urls"] = string.Join(",", request.BlockedUrls),
            };
            var uri = QueryHelpers("ulr-filter", query);

            var response = await _httpClient.GetFromJsonAsync<GatewayFilterResponse>(uri, ct);

            var matchedKeywords = response?.UniqueElements ?? Array.Empty<string>();
            var matchedUrls = response?.UrlUniqueElements ?? Array.Empty<string>();

            return new SpamFilterResult(
                IsBlocked: matchedKeywords.Length > 0 || matchedUrls.Length > 0,
                MatchedKeywords: matchedKeywords,
                MatchedUrls: matchedUrls);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Spam preflight filter call failed; failing open (not blocked)");
            return new SpamFilterResult(false, Array.Empty<string>(), Array.Empty<string>());
        }
    }

    public async Task<GatewaySendResult> SendAsync(string message, string mobileNumber, string senderId, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["message"] = message,
            ["mobiles_numbers"] = mobileNumber,
            ["sender_id"] = senderId,
        };
        var uri = QueryHelpers(string.Empty, query);

        var response = await _httpClient.GetAsync(uri, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        return new GatewaySendResult(
            Success: response.IsSuccessStatusCode,
            ExternalMessageId: ExtractMessageId(raw),
            RawResponse: raw);
    }

    private static string? ExtractMessageId(string raw)
    {
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<GatewaySendResponse>(raw);
            return parsed?.MessageId;
        }
        catch
        {
            return null;
        }
    }

    private static string QueryHelpers(string path, Dictionary<string, string?> query)
    {
        var pairs = query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        var queryString = string.Join("&", pairs);
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    private class GatewayFilterResponse
    {
        public string[]? UniqueElements { get; set; }
        public string[]? UrlUniqueElements { get; set; }
    }

    private class GatewaySendResponse
    {
        public string? MessageId { get; set; }
    }
}
