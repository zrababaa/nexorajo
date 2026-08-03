using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class HistoryResendService : IHistoryResendService
{
    private static readonly MessageStatus[] ResendableStatuses =
    {
        MessageStatus.Failed,
        MessageStatus.Undelivered,
        MessageStatus.Expired,
    };

    private readonly SmppDbContext _db;
    private readonly SendCore _sendCore;

    public HistoryResendService(SmppDbContext db, SendCore sendCore)
    {
        _db = db;
        _sendCore = sendCore;
    }

    public async Task<SendSummaryDto> ResendAsync(int historyId, MessageSource source, int currentUserId, CancellationToken ct = default)
    {
        var original = await FindAsync(historyId, source, ct)
            ?? throw new AppException("Message not found.");

        if (original.CreatedByUserId != currentUserId)
        {
            throw new AppException("You can only resend your own messages.");
        }

        if (!ResendableStatuses.Contains(original.Status))
        {
            throw new AppException("Only failed, undelivered, or expired messages can be resent.");
        }

        if (string.IsNullOrEmpty(original.MessageText))
        {
            throw new AppException("The original message text is unavailable for this entry.");
        }

        return await _sendCore.ExecuteAsync(
            currentUserId,
            new[] { original.ReceiverNumber },
            original.MessageText,
            original.SenderNumber,
            source,
            ResolveTransactionSource(source),
            ct);
    }

    /// <summary>
    /// The row is in whichever of the daemon's two log tables its source says it is - see
    /// IHistoryResendService.ResendAsync. Only the handful of fields a resend needs are read, so
    /// the two shapes collapse to one here.
    /// </summary>
    private async Task<ResendSource?> FindAsync(int historyId, MessageSource source, CancellationToken ct)
    {
        if (source == MessageSource.QuickSend)
        {
            return await _db.QuickSendHistories
                .AsNoTracking()
                .Where(h => h.Id == historyId)
                .Select(h => new ResendSource(h.CreatedByUserId, h.Status, h.MessageText, h.ReceiverNumber, h.SenderNumber))
                .FirstOrDefaultAsync(ct);
        }

        return await _db.Histories
            .AsNoTracking()
            .Where(h => h.Id == historyId && h.Source == source)
            .Select(h => new ResendSource(h.CreatedByUserId, h.Status, h.MessageText, h.ReceiverNumber, h.SenderNumber))
            .FirstOrDefaultAsync(ct);
    }

    private static TransactionSource ResolveTransactionSource(MessageSource source) => source switch
    {
        MessageSource.QuickSend => TransactionSource.QuickSend,
        MessageSource.BulkSend => TransactionSource.BulkSend,
        MessageSource.PublicApi => TransactionSource.PublicApi,
        _ => TransactionSource.QuickSend,
    };

    private record ResendSource(
        int CreatedByUserId, MessageStatus Status, string? MessageText, string ReceiverNumber, string SenderNumber);
}
