using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.SmsGateway;

/// <summary>
/// Thin HTTP client over the external, out-of-repo gateway at legacy's SMPP_API_URL. Despite
/// the name that host is not how messages are sent - sending happens by inserting an
/// UnderProcess row for the SMPP daemon to drain. The only live endpoint here is the
/// spam/URL preflight filter, which is exactly how legacy used it too.
///
/// Wire contract (verb, param names, JSON casing) is reverse-engineered from the legacy Laravel
/// app's curl calls (app/Http/Controllers/Whatsapp.php) against this same gateway - it is an
/// external system we don't own, so it must match exactly, not what would be more idiomatic here.
/// </summary>
public class SmsGatewayClient : ISmsGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsGatewayClient> _logger;

    public SmsGatewayClient(HttpClient httpClient, IOptions<GatewayOptions> options, ILogger<SmsGatewayClient> logger)
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
                ["inc_keyword"] = string.Join(",", request.IncludeKeywords),
                ["exc_keyword"] = string.Join(",", request.ExcludeKeywords),
                ["exc_urls"] = string.Join(",", request.BlockedUrls),
                ["msg"] = request.Message,
            };
            var uri = BuildQuery("ulr-filter", query);

            using var response = await _httpClient.PostAsync(uri, content: null, ct);
            var body = await response.Content.ReadFromJsonAsync<GatewayFilterResponse>(cancellationToken: ct);

            var matchedKeywords = body?.UniqueElements ?? Array.Empty<string>();
            var matchedUrls = body?.UrlUniqueElements ?? Array.Empty<string>();

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

    private static string BuildQuery(string path, Dictionary<string, string?> query)
    {
        var pairs = query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        var queryString = string.Join("&", pairs);
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    private class GatewayFilterResponse
    {
        [JsonPropertyName("unique_elements")]
        public string[]? UniqueElements { get; set; }

        [JsonPropertyName("url_unique_elements")]
        public string[]? UrlUniqueElements { get; set; }
    }
}
