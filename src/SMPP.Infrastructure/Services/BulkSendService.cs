using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class BulkSendService : IBulkSendService
{
    private readonly SmppDbContext _db;
    private readonly SendCore _sendCore;

    public BulkSendService(SmppDbContext db, SendCore sendCore)
    {
        _db = db;
        _sendCore = sendCore;
    }

    public async Task<SendSummaryDto> SubmitAsync(int userId, BulkSendRequest request, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.CreatedByUserId == userId, ct)
            ?? throw new AppException("Campaign not found.");

        var numbers = campaign.Numbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var numberToMessage = await TemplateMessageResolver.ResolveAsync(
            _db, userId, numbers, request.Message, request.TemplateId, request.TemplateVariables, ct);

        return await _sendCore.ExecuteGroupedAsync(userId, numberToMessage, request.SenderId, MessageSource.BulkSend, TransactionSource.BulkSend, ct, campaign.Name);
    }
}
