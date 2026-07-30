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

    public async Task<SendSummaryDto> ResendAsync(int historyId, int currentUserId, CancellationToken ct = default)
    {
        var history = await _db.Histories.FirstOrDefaultAsync(h => h.Id == historyId, ct)
            ?? throw new AppException("Message not found.");

        if (history.CreatedByUserId != currentUserId)
        {
            throw new AppException("You can only resend your own messages.");
        }

        if (!ResendableStatuses.Contains(history.Status))
        {
            throw new AppException("Only failed, undelivered, or expired messages can be resent.");
        }

        if (string.IsNullOrEmpty(history.MessageText))
        {
            throw new AppException("The original message text is unavailable for this entry.");
        }

        return await _sendCore.ExecuteAsync(
            currentUserId,
            new[] { history.ReceiverNumber },
            history.MessageText,
            history.SenderNumber,
            history.Source,
            ResolveTransactionSource(history.Source),
            ct);
    }

    private static TransactionSource ResolveTransactionSource(MessageSource source) => source switch
    {
        MessageSource.QuickSend => TransactionSource.QuickSend,
        MessageSource.BulkSend => TransactionSource.BulkSend,
        MessageSource.PublicApi => TransactionSource.PublicApi,
        _ => TransactionSource.QuickSend,
    };
}
