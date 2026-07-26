namespace SMPP.Application.Sending;

/// <summary>Sends to every number in a saved Campaign (recipient list) the caller owns.</summary>
public interface IBulkSendService
{
    Task<SendSummaryDto> SubmitAsync(int userId, BulkSendRequest request, CancellationToken ct = default);
}
