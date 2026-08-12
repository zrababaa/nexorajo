using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SMPP.Application.LinkTracking;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class LinkTrackingService : ILinkTrackingService
{
    private const string TokenAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int TokenLength = 10;

    private readonly SmppDbContext _db;
    private readonly LinkTrackingOptions _options;

    public LinkTrackingService(SmppDbContext db, IOptions<LinkTrackingOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public Task<string> RewriteMessageAsync(string message, string batchId, int userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
        {
            return Task.FromResult(message);
        }

        var urls = UrlExtractor.ExtractDistinct(message);
        if (urls.Count == 0)
        {
            return Task.FromResult(message);
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
            message = message.Replace(url, $"{_options.BaseUrl}/l/{token}");
        }

        return Task.FromResult(message);
    }

    private static string? TryGetHost(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;

    private static string NewToken() => RandomNumberGenerator.GetString(TokenAlphabet, TokenLength);
}
