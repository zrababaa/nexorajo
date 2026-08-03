using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.PublicApi;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;

namespace SMPP.Infrastructure.Services;

public class PublicApiSendService : IPublicApiSendService
{
    private readonly ICampaignNumberParser _numberParser;
    private readonly SendCore _sendCore;

    public PublicApiSendService(ICampaignNumberParser numberParser, SendCore sendCore)
    {
        _numberParser = numberParser;
        _sendCore = sendCore;
    }

    public async Task<SendSummaryDto> SubmitAsync(int userId, PublicApiSendRequest request, CancellationToken ct = default)
    {
        var parsed = _numberParser.ParsePasted(request.Numbers);
        if (parsed.Count == 0)
        {
            throw new AppException("No valid phone numbers were found in the input provided.");
        }

        // An absent sender_id, and whether the caller may use the one it asked for, are both
        // settled by SendCore against the account's Sender ID policy.
        var numbers = parsed.NormalizedNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return await _sendCore.ExecuteAsync(
            userId, numbers, request.Message, request.SenderId ?? string.Empty, MessageSource.PublicApi, TransactionSource.PublicApi, ct);
    }
}
