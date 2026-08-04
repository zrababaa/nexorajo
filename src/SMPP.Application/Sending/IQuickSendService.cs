namespace SMPP.Application.Sending;

/// <summary>
/// Ad-hoc numbers + message send. Cost = recipientCount x segments x MessagePricing,
/// deducted once via IBalanceLedgerService (legacy deducted once per 25-number chunk, which
/// silently over-charged any batch bigger than 25 numbers - not reproduced here). Superadmin
/// sends for free.
/// </summary>
public interface IQuickSendService
{
    Task<SendSummaryDto> SubmitAsync(int userId, QuickSendRequest request, CancellationToken ct = default);
}
