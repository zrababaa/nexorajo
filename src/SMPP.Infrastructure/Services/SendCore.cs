using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Shared debit+filter+queue logic used by QuickSendService, BulkSendService, and the public
/// API send endpoint - only the number-resolution step (pasted / campaign / API payload)
/// differs between callers, everything after that is identical.
/// </summary>
public class SendCore
{
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
        CancellationToken ct)
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

        var batchId = Guid.NewGuid().ToString("N");

        if (totalCost > 0)
        {
            await _ledger.ApplyAsync(new BalanceLedgerRequest(userId, userId, totalCost, TransactionKind.Debit, txSource, batchId), ct);
        }

        foreach (var number in numbers)
        {
            _db.OutboundMessages.Add(new OutboundMessage
            {
                CampaignBatchId = batchId,
                Source = source,
                SenderNumber = senderId,
                ReceiverNumber = number,
                MessageText = message,
                CreatedByUserId = userId,
            });
        }
        await _db.SaveChangesAsync(ct);

        var remainingBalance = await _db.Users.Where(u => u.Id == userId).Select(u => u.Balance).FirstAsync(ct);
        return new SendSummaryDto(batchId, numbers.Count, segments, totalCost, remainingBalance);
    }
}
