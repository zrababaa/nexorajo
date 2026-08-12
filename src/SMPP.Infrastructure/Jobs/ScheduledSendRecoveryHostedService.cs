using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Services;

namespace SMPP.Infrastructure.Jobs;

/// <summary>
/// Quartz's default job store is in-memory, so every pending scheduled send needs its trigger
/// re-armed after a restart/redeploy - the <c>ScheduledSends</c> table (not Quartz) is the durable
/// source of truth. Runs once at startup; anything already overdue while the app was down fires
/// immediately instead of being lost.
/// </summary>
public class ScheduledSendRecoveryHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<ScheduledSendRecoveryHostedService> _logger;

    public ScheduledSendRecoveryHostedService(
        IServiceScopeFactory scopeFactory, ISchedulerFactory schedulerFactory, ILogger<ScheduledSendRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmppDbContext>();

        var pending = await db.ScheduledSends.AsNoTracking()
            .Where(s => s.Status == ScheduledSendStatus.Pending)
            .Select(s => new { s.Id, s.ScheduledAtUtc })
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return;
        }

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        foreach (var scheduledSend in pending)
        {
            await scheduler.ScheduleJob(
                ScheduledSendService.BuildJob(scheduledSend.Id),
                ScheduledSendService.BuildTrigger(scheduledSend.Id, scheduledSend.ScheduledAtUtc),
                ct);
        }

        _logger.LogInformation("Re-armed {Count} pending scheduled send(s) after startup.", pending.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
