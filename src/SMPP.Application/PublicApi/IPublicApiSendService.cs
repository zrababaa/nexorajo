using SMPP.Application.Sending;

namespace SMPP.Application.PublicApi;

public record PublicApiSendRequest(string Numbers, string Message, string? SenderId);

/// <summary>
/// Ports legacy's SendMessageApi::sendMessage endpoint contract onto the shared send pipeline
/// (SendCore) - same balance/segment/spam-filter rules as Quick Send and Bulk Send, tagged
/// with Source.PublicApi so it shows up separately in history/reporting.
/// </summary>
public interface IPublicApiSendService
{
    Task<SendSummaryDto> SubmitAsync(int userId, PublicApiSendRequest request, CancellationToken ct = default);
}
