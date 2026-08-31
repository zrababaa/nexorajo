using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Common;
using SMPP.Application.LinkTracking;
using SMPP.Application.UrlShortening;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class LinkTrackingService : ILinkTrackingService
{
    private const string TokenAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int TokenLength = 10;

    private readonly SmppDbContext _db;
    private readonly LinkTrackingOptions _options;
    private readonly IUrlShortenerService _urlShortener;
    private readonly ILogger<LinkTrackingService> _logger;

    public LinkTrackingService(
        SmppDbContext db,
        IOptions<LinkTrackingOptions> options,
        IUrlShortenerService urlShortener,
        ILogger<LinkTrackingService> logger)
    {
        _db = db;
        _options = options.Value;
        _urlShortener = urlShortener;
        _logger = logger;
    }

    public async Task<string> RewriteMessageAsync(
        string message, string batchId, int userId, bool shortenLinks = false, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
        {
            return message;
        }

        var urls = UrlExtractor.ExtractDistinct(message);
        if (urls.Count == 0)
        {
            return message;
        }

        var ownHost = TryGetHost(_options.BaseUrl);
        var replacements = new List<(string Url, string Token)>();

        foreach (var url in urls)
        {
            if (ownHost is not null && string.Equals(TryGetHost(url), ownHost, StringComparison.OrdinalIgnoreCase))
            {
                // Already one of our own tracking links - e.g. a resend of a previously-rewritten
                // message (see HistoryResendService). Leave it as-is instead of wrapping it in a
                // second layer of redirect that would grow with every resend.
                continue;
            }

            var token = NewToken();
            _db.TrackedLinks.Add(new TrackedLink
            {
                Token = token,
                BatchId = batchId,
                DestinationUrl = url,
                CreatedByUserId = userId,
            });

            replacements.Add((url, token));
        }

        // Longest URL first: if one extracted URL is a literal prefix of another (e.g.
        // "https://x.com" and "https://x.com/y" both appear in the same message),
        // replacing the shorter one first would corrupt the longer one's occurrence too.
        foreach (var (url, token) in replacements.OrderByDescending(r => r.Url.Length))
        {
            var trackingUrl = $"{_options.BaseUrl}/l/{token}";
            var replacement = shortenLinks
                ? await ShortenOrFallbackAsync(trackingUrl, batchId, ct)
                : trackingUrl;

            message = message.Replace(url, replacement);
        }

        return message;
    }

    /// <summary>
    /// The send path must not fail just because the shortener is misconfigured or briefly down:
    /// on any shortening error the recipient still gets a working (if longer) tracking link, and
    /// the click still lands on <c>/l/{token}</c>. A real cancellation is not swallowed - it
    /// surfaces as <see cref="OperationCanceledException"/>, which this catch does not match.
    /// </summary>
    private async Task<string> ShortenOrFallbackAsync(string trackingUrl, string batchId, CancellationToken ct)
    {
        try
        {
            return await _urlShortener.ShortenUrlAsync(trackingUrl, ct);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(
                ex, "Could not shorten the tracking link for batch {BatchId}; using the full tracking URL.", batchId);
            return trackingUrl;
        }
    }

    private static string? TryGetHost(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;

    private static string NewToken() => RandomNumberGenerator.GetString(TokenAlphabet, TokenLength);
}
