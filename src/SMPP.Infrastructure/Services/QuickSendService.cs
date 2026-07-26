using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Ports legacy Whatsapp::submitQuickSend's shape (normalize+dedup numbers, spam-check the
/// message, deduct balance, queue one row per recipient) but fixes its balance bug: legacy
/// re-ran the full deduction once per 25-number chunk, over-charging any batch above 25
/// numbers. SendCore computes and debits the cost once for the whole batch.
/// </summary>
public class QuickSendService : IQuickSendService
{
    private readonly ICampaignNumberParser _numberParser;
    private readonly SendCore _sendCore;

    public QuickSendService(ICampaignNumberParser numberParser, SendCore sendCore)
    {
        _numberParser = numberParser;
        _sendCore = sendCore;
    }

    public Task<SendSummaryDto> SubmitAsync(int userId, QuickSendRequest request, CancellationToken ct = default)
    {
        var parsed = _numberParser.ParsePasted(request.RawNumbers);
        if (parsed.Count == 0)
        {
            throw new AppException("No valid phone numbers were found in the input provided.");
        }

        var numbers = parsed.NormalizedNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return _sendCore.ExecuteAsync(userId, numbers, request.Message, request.SenderId, MessageSource.QuickSend, TransactionSource.QuickSend, ct);
    }
}
