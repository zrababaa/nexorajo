using Microsoft.EntityFrameworkCore;
using Quartz;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Application.SendingWindow;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Jobs;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class ScheduledSendService : IScheduledSendService
{
    private readonly SmppDbContext _db;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ISendingWindowService _sendingWindow;

    public ScheduledSendService(SmppDbContext db, ISchedulerFactory schedulerFactory, ISendingWindowService sendingWindow)
    {
        _db = db;
        _schedulerFactory = schedulerFactory;
        _sendingWindow = sendingWindow;
    }

    public async Task<int> CreateAsync(int userId, CreateScheduledSendRequest request, CancellationToken ct = default)
    {
        var campaignOwned = await _db.Campaigns.AnyAsync(c => c.Id == request.CampaignId && c.CreatedByUserId == userId, ct);
        if (!campaignOwned)
        {
            throw new AppException("Campaign not found.");
        }

        // Interpreted as server local time, matching how the sending window's own start/end times
        // are interpreted (see ISendingWindowService) - there is no per-user timezone in this app.
        var scheduledAtUtc = DateTime.SpecifyKind(request.ScheduledAtLocal, DateTimeKind.Local).ToUniversalTime();
        if (scheduledAtUtc <= DateTime.UtcNow)
        {
            throw new AppException("The scheduled time must be in the future.");
        }

        var window = await _sendingWindow.GetAsync(ct);
        var allowed = await _sendingWindow.IsTimeOfDayAllowedAsync(TimeOnly.FromDateTime(request.ScheduledAtLocal), ct);
        if (!allowed)
        {
            throw new AppException(
                $"That time falls outside the allowed Bulk Send window ({window.StartTime:HH:mm}-{window.EndTime:HH:mm}).");
        }

        var scheduledSend = new ScheduledSend
        {
            CampaignId = request.CampaignId,
            Message = request.Message,
            SenderId = request.SenderId,
            ScheduledAtUtc = scheduledAtUtc,
            Status = ScheduledSendStatus.Pending,
            CreatedByUserId = userId,
        };

        _db.ScheduledSends.Add(scheduledSend);
        await _db.SaveChangesAsync(ct);

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        await scheduler.ScheduleJob(BuildJob(scheduledSend.Id), BuildTrigger(scheduledSend.Id, scheduledAtUtc), ct);

        return scheduledSend.Id;
    }

    public Task<ScheduledSendListItemDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default) =>
        ListQuery(userId).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PagedResult<ScheduledSendListItemDto>> GetPagedAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = ListQuery(userId).OrderByDescending(s => s.ScheduledAtUtc);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ScheduledSendListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task CancelAsync(int id, int userId, CancellationToken ct = default)
    {
        var scheduledSend = await _db.ScheduledSends.FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId, ct)
            ?? throw new AppException("Scheduled send not found.");

        if (scheduledSend.Status != ScheduledSendStatus.Pending)
        {
            throw new AppException("Only a still-pending scheduled send can be cancelled.");
        }

        scheduledSend.Status = ScheduledSendStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        await scheduler.DeleteJob(JobKeyFor(id), ct);
    }

    private IQueryable<ScheduledSendListItemDto> ListQuery(int userId) =>
        from s in _db.ScheduledSends.AsNoTracking()
        join c in _db.Campaigns.AsNoTracking() on s.CampaignId equals c.Id
        where s.CreatedByUserId == userId
        select new ScheduledSendListItemDto(s.Id, c.Name, s.Message, s.SenderId, s.ScheduledAtUtc, s.Status, s.BatchId, s.ErrorMessage);

    internal static JobKey JobKeyFor(int scheduledSendId) => new($"scheduled-send-{scheduledSendId}", "scheduled-sends");

    internal static IJobDetail BuildJob(int scheduledSendId) =>
        JobBuilder.Create<ScheduledSendDispatchJob>()
            .WithIdentity(JobKeyFor(scheduledSendId))
            .UsingJobData(ScheduledSendDispatchJob.ScheduledSendIdKey, scheduledSendId)
            .Build();

    internal static ITrigger BuildTrigger(int scheduledSendId, DateTime scheduledAtUtc)
    {
        var builder = TriggerBuilder.Create().WithIdentity($"trigger-{scheduledSendId}", "scheduled-sends");
        return (scheduledAtUtc <= DateTime.UtcNow
            ? builder.StartNow()
            : builder.StartAt(new DateTimeOffset(scheduledAtUtc, TimeSpan.Zero))).Build();
    }
}
