using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Common;
using SMPP.Application.UrlShortening;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// <see cref="IUrlShortenerService"/> backed by Short.io's REST API. Registered as a typed
/// HttpClient (see DependencyInjection.AddInfrastructure) so IHttpClientFactory owns the handler
/// lifetime. The API key comes from configuration (<c>ShortIo:ApiKey</c>) and is sent in the
/// Authorization header only - it is never written to logs or surfaced in an exception.
///
/// A Short.io <b>secret</b> key uses <c>POST /links</c>; a <b>public</b> key (prefixed
/// <c>pk_</c>) is only accepted by <c>POST /links/public</c>. Both take the same
/// <c>{originalURL, domain}</c> body and return the same <c>shortURL</c>, so the only difference
/// is the path, picked from the key prefix.
/// </summary>
public sealed class ShortIoUrlShortenerService : IUrlShortenerService
{
    private const string PublicKeyPrefix = "pk_";

    /// <summary>Paths relative to the client's <c>https://api.short.io/</c> base address.</summary>
    private const string SecretKeyLinkPath = "links";
    private const string PublicKeyLinkPath = "links/public";

    private const int MaxLoggedBodyChars = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ShortIoOptions _options;
    private readonly ILogger<ShortIoUrlShortenerService> _logger;

    public ShortIoUrlShortenerService(
        HttpClient http,
        IOptions<ShortIoOptions> options,
        ILogger<ShortIoUrlShortenerService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ShortenUrlAsync(string originalUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            throw new AppException("A URL to shorten is required.");
        }

        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new AppException("The URL to shorten must be an absolute http or https URL.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.Domain))
        {
            _logger.LogError("Short.io is not configured: set ShortIo:ApiKey and ShortIo:Domain.");
            throw new AppException("URL shortening is not configured.");
        }

        var path = _options.ApiKey.StartsWith(PublicKeyPrefix, StringComparison.Ordinal)
            ? PublicKeyLinkPath
            : SecretKeyLinkPath;

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(
                new ShortIoCreateLinkRequest(parsed.ToString(), _options.Domain), options: SerializerOptions),
        };
        // Short.io authenticates with the raw key in the Authorization header - no "Bearer" scheme.
        request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // ex.Message can name the host but never the Authorization header, so it is safe to log.
            _logger.LogWarning(ex, "Short.io request failed before a response was received.");
            throw new AppException("The URL shortening service is unavailable. Please try again shortly.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Short.io request timed out.");
            throw new AppException("The URL shortening service timed out. Please try again shortly.");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // body is Short.io's own error payload; it does not contain our API key.
                _logger.LogWarning(
                    "Short.io link creation returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    Truncate(body, MaxLoggedBodyChars));
                throw new AppException($"URL shortening failed ({(int)response.StatusCode}).");
            }

            ShortIoLinkResponse? link;
            try
            {
                link = JsonSerializer.Deserialize<ShortIoLinkResponse>(body, SerializerOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Short.io returned a response that could not be parsed as JSON.");
                throw new AppException("URL shortening returned an unexpected response.");
            }

            var shortUrl = link?.SecureShortUrl ?? link?.ShortUrl;
            if (string.IsNullOrWhiteSpace(shortUrl))
            {
                _logger.LogWarning("Short.io response contained no short URL.");
                throw new AppException("URL shortening returned an unexpected response.");
            }

            return shortUrl;
        }
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

    /// <summary>The request body Short.io's <c>POST /links</c> expects: the long URL and the short domain to mint it under.</summary>
    private sealed record ShortIoCreateLinkRequest(
        [property: JsonPropertyName("originalURL")] string OriginalUrl,
        [property: JsonPropertyName("domain")] string Domain);
}
