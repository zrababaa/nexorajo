using SMPP.Application.Sending;
using SMPP.Domain.Enums;

namespace SMPP.Application.History;

/// <summary>
/// Re-queues a single history row (Failed/Undelivered/Expired only) through the same SendCore
/// pipeline as Quick/Bulk Send - spam-filters, debits, and hands it to the SMPP daemon as a
/// brand new single-recipient batch rather than mutating the original row.
/// </summary>
public interface IHistoryResendService
{
    /// <summary>
    /// <paramref name="source"/> is required as well as the id because the daemon logs to two
    /// tables and the ids are only unique within each: Quick Send rows live in
    /// <c>quick_send_history</c>, everything else in <c>historys</c>. Without it an id from one
    /// table would silently resolve to an unrelated row in the other.
    /// </summary>
    Task<SendSummaryDto> ResendAsync(int historyId, MessageSource source, int currentUserId, CancellationToken ct = default);
}
