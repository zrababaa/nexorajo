using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.PublicApi;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class PublicApiSendService : IPublicApiSendService
{
    private readonly SmppDbContext _db;
    private readonly ICampaignNumberParser _numberParser;
    private readonly SendCore _sendCore;

    public PublicApiSendService(SmppDbContext db, ICampaignNumberParser numberParser, SendCore sendCore)
    {
        _db = db;
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

        var senderId = request.SenderId;
        if (string.IsNullOrWhiteSpace(senderId))
        {
            senderId = await _db.Users.Where(u => u.Id == userId).Select(u => u.SenderId).FirstAsync(ct)
                ?? throw new AppException("No sender_id provided and the account has no default Sender ID configured.");
        }

        var numbers = parsed.NormalizedNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return await _sendCore.ExecuteAsync(userId, numbers, request.Message, senderId, MessageSource.PublicApi, TransactionSource.PublicApi, ct);
    }
}
