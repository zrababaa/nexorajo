using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Services;

namespace SMPP.Infrastructure.Jobs;

/// <summary>
/// Fires a scheduled Bulk Send at the exact time its Quartz trigger goes off. Reuses the same
/// SendCore pipeline as an immediate Bulk Send - pricing, spam filter, sender-ID policy, and the
/// admin sending-window check all apply identically here.
/// </summary>
public class ScheduledSendDispatchJob : IJob
{
    public const string ScheduledSendIdKey = "scheduledSendId";

    private readonly SmppDbContext _db;
    private readonly SendCore _sendCore;

    public ScheduledSendDispatchJob(SmppDbContext db, SendCore sendCore)
    {
        _db = db;
        _sendCore = sendCore;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var id = context.JobDetail.JobDataMap.GetInt(ScheduledSendIdKey);
        var ct = context.CancellationToken;

        var scheduledSend = await _db.ScheduledSends.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scheduledSend is null || scheduledSend.Status != ScheduledSendStatus.Pending)
        {
            return;
        }

        var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == scheduledSend.CampaignId, ct);
        if (campaign is null)
        {
            scheduledSend.Status = ScheduledSendStatus.Failed;
            scheduledSend.ErrorMessage = "The campaign this send was scheduled against no longer exists.";
            await _db.SaveChangesAsync(ct);
            return;
        }

        var numbers = campaign.Numbers.Split(',', StringSplitOptions.RemoveEmptyEntries);

        try
        {
            IReadOnlyDictionary<string, string>? templateVariables = scheduledSend.TemplateVariablesJson is null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string>>(scheduledSend.TemplateVariablesJson);

            var numberToMessage = await TemplateMessageResolver.ResolveAsync(
                _db, scheduledSend.CreatedByUserId, numbers, scheduledSend.Message, scheduledSend.TemplateId, templateVariables, ct);

            var summary = await _sendCore.ExecuteGroupedAsync(
                scheduledSend.CreatedByUserId,
                numberToMessage,
                scheduledSend.SenderId,
                MessageSource.BulkSend,
                TransactionSource.BulkSend,
                ct,
                campaign.Name);

            scheduledSend.Status = ScheduledSendStatus.Sent;
            scheduledSend.BatchId = summary.BatchId;
        }
        catch (AppException ex)
        {
            scheduledSend.Status = ScheduledSendStatus.Failed;
            scheduledSend.ErrorMessage = ex.Message;
        }

        await _db.SaveChangesAsync(ct);
    }
}
