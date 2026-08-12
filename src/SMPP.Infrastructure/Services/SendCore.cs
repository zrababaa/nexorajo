using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.LinkTracking;
using SMPP.Application.Sending;
using SMPP.Application.SendingWindow;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Domain.Pricing;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Shared debit+filter+enqueue logic used by QuickSendService, BulkSendService, and the public
/// API send endpoint - only the number-resolution step (pasted / campaign / API payload)
/// differs between callers, everything after that is identical.
///
/// Sending is deliberately fire-and-forget: this writes one <see cref="UnderProcess"/> row and
/// stops. An external SMPP daemon polls that table, submits the messages, and writes the
/// per-recipient History rows back. This app never talks to an SMPP/SOAP endpoint itself.
/// </summary>
public class SendCore
{
    private const string CampaignIdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly SmppDbContext _db;
    private readonly ISegmentCounter _segmentCounter;
    private readonly ISpamKeywordFilterService _spamFilter;
    private readonly ISendPolicyService _sendPolicy;
    private readonly IBalanceLedgerService _ledger;
    private readonly ISendingWindowService _sendingWindow;
    private readonly ILinkTrackingService _linkTracking;

    public SendCore(
        SmppDbContext db,
        ISegmentCounter segmentCounter,
        ISpamKeywordFilterService spamFilter,
        ISendPolicyService sendPolicy,
        IBalanceLedgerService ledger,
        ISendingWindowService sendingWindow,
        ILinkTrackingService linkTracking)
    {
        _db = db;
        _segmentCounter = segmentCounter;
        _spamFilter = spamFilter;
        _sendPolicy = sendPolicy;
        _ledger = ledger;
        _sendingWindow = sendingWindow;
        _linkTracking = linkTracking;
    }

    public async Task<SendSummaryDto> ExecuteAsync(
        int userId,
        IReadOnlyCollection<string> numbers,
        string message,
        string senderId,
        MessageSource source,
        TransactionSource txSource,
        CancellationToken ct,
        string? campaignName = null)
    {
        if (numbers.Count == 0)
        {
            throw new AppException("No valid phone numbers were found.");
        }

        if (source == MessageSource.BulkSend)
        {
            await _sendingWindow.EnsureBulkSendAllowedNowAsync(ct);
        }

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        // Generated here (rather than after the cost calc, where it used to live) because the
        // link-tracking rewrite below needs a batch id to associate TrackedLink rows with - the
        // id itself has no dependency on anything computed later, so moving it up is a no-op
        // behavior change.
        var batchId = NewCampaignId(source);

        var filterResult = await _spamFilter.CheckAsync(message, ct);
        if (filterResult.IsBlocked)
        {
            var matchedTerms = filterResult.MatchedKeywords.Concat(filterResult.MatchedUrls).ToList();
            await _spamFilter.LogBlockedAttemptAsync(userId, source, senderId, numbers.Count, matchedTerms, message, ct);
            throw new SpamBlockedException(matchedTerms);
        }

        // Runs against the message only after it has passed the spam/URL-blocklist check above -
        // rewriting first would let a sender hide a blocked destination behind our own tracking
        // domain. Segment count and cost below run on the rewritten text, since that's what's
        // actually transmitted (and billed for).
        message = await _linkTracking.RewriteMessageAsync(message, batchId, userId, ct);

        senderId = await _sendPolicy.ResolveSenderIdAsync(userId, senderId, ct);

        // One flat credit per message part per recipient (MessagePricing), so a message long
        // enough to split into two parts costs twice as much as a single-part one.
        var segments = _segmentCounter.CountSegments(message);
        var isFree = user.Role == UserRole.Superadmin;
        var totalCost = isFree ? 0m : MessagePricing.CostOf(numbers.Count, segments);

        if (totalCost > user.Balance)
        {
            throw new AppException(
                $"Insufficient balance: this send costs {totalCost:0.####} " +
                $"({numbers.Count} recipient(s) x {segments} message part(s)) and the balance is {user.Balance:0.####}.");
        }

        if (totalCost > 0)
        {
            await _ledger.ApplyAsync(new BalanceLedgerRequest(userId, userId, totalCost, TransactionKind.Debit, txSource, batchId), ct);
        }

        _db.UnderProcessBatches.Add(new UnderProcess
        {
            CampaignId = batchId,
            SenderId = senderId,
            Message = message,
            CampaignNumbers = string.Join(",", numbers),
            CampaignName = campaignName,
            PushType = source == MessageSource.BulkSend ? PushType.CampaignNumbers : PushType.DirectNumbers,
            Priority = source == MessageSource.PublicApi ? SendPriority.Api : SendPriority.Interactive,
            UserId = userId,
        });
        await _db.SaveChangesAsync(ct);

        var remainingBalance = await _db.Users.Where(u => u.Id == userId).Select(u => u.Balance).FirstAsync(ct);
        return new SendSummaryDto(batchId, numbers.Count, segments, totalCost, remainingBalance);
    }

    /// <summary>
    /// Legacy's camp_id shape: a short source prefix plus random characters. Kept under 25
    /// characters because that is the width of the daemon's camp_id column.
    /// </summary>
    private static string NewCampaignId(MessageSource source)
    {
        var prefix = source switch
        {
            MessageSource.BulkSend => "BULK",
            MessageSource.PublicApi => "TXTAPI",
            _ => "QUICK",
        };

        return prefix + RandomNumberGenerator.GetString(CampaignIdAlphabet, 12);
    }
}
