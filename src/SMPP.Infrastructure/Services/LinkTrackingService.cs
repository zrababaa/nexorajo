using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
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

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var ownHost = TryGetHost(_options.BaseUrl);

        // Links minted ahead of this send by PrepareLinkAsync and not yet claimed by a batch. If
        // one turns up in the message it is already a tracking link, so this send just claims it
        // (attaches its batch id) instead of wrapping it in a second redirect. Matched by the token
        // in a "/l/{token}" URL or by the whole short URL, so the lookup stays bounded by the number
        // of links in this one message rather than every unclaimed link the user ever composed.
        var candidateTokens = urls
            .Select(u => TryGetTrackingToken(u, ownHost))
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();

        var urlList = urls.ToList();
        var claimable = await _db.TrackedLinks
            .Where(t => t.BatchId == "" && t.CreatedByUserId == userId
                && (candidateTokens.Contains(t.Token) || urlList.Contains(t.ShortUrl)))
            .ToListAsync(ct);

        var newLinks = new List<(string Url, TrackedLink Row)>();

        foreach (var url in urls)
        {
            var prepared = claimable.FirstOrDefault(t =>
                string.Equals(url, t.ShortUrl, StringComparison.OrdinalIgnoreCase)
                || string.Equals(url, $"{baseUrl}/l/{t.Token}", StringComparison.OrdinalIgnoreCase));

            if (prepared is not null)
            {
                prepared.BatchId = batchId;
                continue;
            }

            if (ownHost is not null && string.Equals(TryGetHost(url), ownHost, StringComparison.OrdinalIgnoreCase))
            {
                // Already one of our own tracking links - e.g. a resend of a previously-rewritten
                // message (see HistoryResendService). Leave it as-is instead of wrapping it in a
                // second layer of redirect that would grow with every resend.
                continue;
            }

            var row = new TrackedLink
            {
                Token = NewToken(),
                BatchId = batchId,
                DestinationUrl = url,
                CreatedByUserId = userId,
            };
            _db.TrackedLinks.Add(row);
            newLinks.Add((url, row));
        }

        // Longest URL first: if one extracted URL is a literal prefix of another (e.g.
        // "https://x.com" and "https://x.com/y" both appear in the same message),
        // replacing the shorter one first would corrupt the longer one's occurrence too.
        foreach (var (url, row) in newLinks.OrderByDescending(r => r.Url.Length))
        {
            var trackingUrl = $"{baseUrl}/l/{row.Token}";
            var replacement = trackingUrl;

            if (shortenLinks)
            {
                var shortened = await ShortenOrFallbackAsync(trackingUrl, $"batch {batchId}", ct);
                if (!string.Equals(shortened, trackingUrl, StringComparison.Ordinal))
                {
                    row.ShortUrl = shortened;
                    replacement = shortened;
                }
            }

            message = message.Replace(url, replacement);
        }

        return message;
    }

    public async Task<PreparedLinkDto> PrepareLinkAsync(
        string destinationUrl, int userId, bool shorten, CancellationToken ct = default)
    {
        var url = (destinationUrl ?? string.Empty).Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new AppException("Enter a full http:// or https:// link to track.");
        }

        if (string.IsNullOrEmpty(_options.BaseUrl))
        {
            throw new AppException("Link tracking is not configured.");
        }

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var ownHost = TryGetHost(_options.BaseUrl);

        if (ownHost is not null && string.Equals(parsed.Host, ownHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("That link already points at the tracking domain.");
        }

        var token = NewToken();
        var trackingUrl = $"{baseUrl}/l/{token}";

        string? shortUrl = null;
        if (shorten)
        {
            var shortened = await ShortenOrFallbackAsync(trackingUrl, "a composed link", ct);
            if (!string.Equals(shortened, trackingUrl, StringComparison.Ordinal))
            {
                shortUrl = shortened;
            }
        }

        _db.TrackedLinks.Add(new TrackedLink
        {
            Token = token,
            BatchId = string.Empty,
            DestinationUrl = url,
            ShortUrl = shortUrl,
            CreatedByUserId = userId,
        });
        await _db.SaveChangesAsync(ct);

        return new PreparedLinkDto(token, shortUrl ?? trackingUrl, url, shortUrl is not null);
    }

    /// <summary>
    /// Shortening must not be a hard dependency: if the shortener is misconfigured or briefly down,
    /// the caller still gets a working (if longer) <c>/l/{token}</c> tracking link and the click is
    /// still counted. A real cancellation is not swallowed - it surfaces as
    /// <see cref="OperationCanceledException"/>, which this catch does not match.
    /// </summary>
    private async Task<string> ShortenOrFallbackAsync(string trackingUrl, string context, CancellationToken ct)
    {
        try
        {
            return await _urlShortener.ShortenUrlAsync(trackingUrl, ct);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(
                ex, "Could not shorten the tracking link for {Context}; using the full tracking URL.", context);
            return trackingUrl;
        }
    }

    private static string? TryGetHost(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;

    /// <summary>The <c>{token}</c> from an <c>https://{ownHost}/l/{token}</c> URL, or null if it isn't one.</summary>
    private static string? TryGetTrackingToken(string url, string? ownHost)
    {
        if (ownHost is null || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Host, ownHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["l", var token] ? token : null;
    }

    private static string NewToken() => RandomNumberGenerator.GetString(TokenAlphabet, TokenLength);
}
