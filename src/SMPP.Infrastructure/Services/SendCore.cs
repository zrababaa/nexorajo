using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
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
    private readonly IBalanceLedgerService _ledger;

    public SendCore(SmppDbContext db, ISegmentCounter segmentCounter, ISpamKeywordFilterService spamFilter, IBalanceLedgerService ledger)
    {
        _db = db;
        _segmentCounter = segmentCounter;
        _spamFilter = spamFilter;
        _ledger = ledger;
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

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        var filterResult = await _spamFilter.CheckAsync(message, ct);
        if (filterResult.IsBlocked)
        {
            var matches = filterResult.MatchedKeywords.Concat(filterResult.MatchedUrls);
            throw new AppException($"Message blocked by content filter: {string.Join(", ", matches)}");
        }

        var segments = _segmentCounter.CountSegments(message);
        var isFree = user.Role == UserRole.Superadmin;
        var totalCost = isFree ? 0m : numbers.Count * segments * user.RatePerMessage;

        var batchId = NewCampaignId(source);

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
