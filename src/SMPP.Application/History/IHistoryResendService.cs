using SMPP.Application.Sending;

namespace SMPP.Application.History;

/// <summary>
/// Re-queues a single History row (Failed/Undelivered/Expired only) through the same
/// SendCore pipeline as Quick/Bulk Send - spam-filters, debits, and hands it to the SMPP
/// daemon as a brand new single-recipient batch rather than mutating the original row.
/// </summary>
public interface IHistoryResendService
{
    Task<SendSummaryDto> ResendAsync(int historyId, int currentUserId, CancellationToken ct = default);
}
