using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Abstractions;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Email;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Matches the message against the keyword lists in-process - see <see cref="SpamKeywordMatcher"/>
/// for the rules themselves.
///
/// Legacy delegated this to the external gateway's <c>ulr-filter</c> endpoint and treated any
/// element it returned - from either keyword list - as a block. That host is frequently
/// unreachable from the app server, and the call carried a 30 second timeout that then failed
/// open, so in practice no message was ever filtered and every send stalled for half a minute
/// first. The matching is a substring scan over a handful of rows, so it runs here instead.
///
/// Both keyword lists block, exactly as legacy behaved: the Include/Exclude split is how the
/// operator categorises the words, not two different verdicts.
/// </summary>
public class SpamKeywordFilterService : ISpamKeywordFilterService
{
    private readonly SmppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<SpamKeywordFilterService> _logger;

    public SpamKeywordFilterService(
        SmppDbContext db,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<SpamKeywordFilterService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<SpamFilterResult> CheckAsync(string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new SpamFilterResult(false, Array.Empty<string>(), Array.Empty<string>());
        }

        var keywords = await _db.SpamKeywords
            .AsNoTracking()
            .Where(k => k.IsEnabled)
            .Select(k => new { k.Keyword, k.KeywordType })
            .ToListAsync(ct);

        var matchedKeywords = keywords
            .Where(k => k.KeywordType != SpamKeywordType.Url && SpamKeywordMatcher.ContainsKeyword(message, k.Keyword))
            .Select(k => k.Keyword)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedUrls = keywords
            .Where(k => k.KeywordType == SpamKeywordType.Url && SpamKeywordMatcher.ContainsUrl(message, k.Keyword))
            .Select(k => k.Keyword)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SpamFilterResult(
            IsBlocked: matchedKeywords.Count > 0 || matchedUrls.Count > 0,
            MatchedKeywords: matchedKeywords,
            MatchedUrls: matchedUrls);
    }

    public async Task LogBlockedAttemptAsync(
        int userId,
        MessageSource source,
        string senderId,
        int recipientCount,
        IReadOnlyCollection<string> matchedTerms,
        string message,
        CancellationToken ct = default)
    {
        _db.SpamBlockedAttempts.Add(new SpamBlockedAttempt
        {
            UserId = userId,
            Source = source,
            SenderId = senderId,
            RecipientCount = recipientCount,
            MatchedTerms = string.Join(", ", matchedTerms),
            Message = message,
        });
        await _db.SaveChangesAsync(ct);

        await SendAlertEmailAsync(userId, source, senderId, recipientCount, matchedTerms, message, ct);
    }

    /// <summary>Best-effort: an SMTP outage must never surface as a failure of the send the user
    /// just made (which is already blocked and reported to them independently of this).</summary>
    private async Task SendAlertEmailAsync(
        int userId,
        MessageSource source,
        string senderId,
        int recipientCount,
        IReadOnlyCollection<string> matchedTerms,
        string message,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.SpamAlertRecipient))
        {
            return;
        }

        try
        {
            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.UserName, u.Email })
                .FirstOrDefaultAsync(ct);

            var subject = $"[SMPP] Spam keyword blocked ({source})";
            var body =
                $"""
                A message was blocked by the spam keyword filter.

                User: {user?.UserName} (#{userId}) <{user?.Email}>
                Source: {source}
                Sender ID: {senderId}
                Recipients: {recipientCount}
                Matched terms: {string.Join(", ", matchedTerms)}

                Message:
                {message}
                """;

            await _emailSender.SendAsync(_emailOptions.SpamAlertRecipient, subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send spam-keyword alert email for user {UserId}", userId);
        }
    }
}
