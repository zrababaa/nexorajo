using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMPP.Application.Abstractions;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Outbound;

/// <summary>
/// The single outbound-send pipeline for the whole app, replacing legacy's four overlapping/
/// mostly-dead mechanisms (an under_process table nobody read, synchronous curl-with-sleep()
/// in the request thread, a never-dispatched queued job, and a scheduled closure/Artisan
/// command pair reading a table nothing ever inserted into). Polls OutboundMessage for Pending
/// rows, claims a batch, sends each through ISmsGatewayClient, and writes one History row
/// per attempt.
/// </summary>
public class OutboundMessageWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboundDispatchOptions _options;
    private readonly ILogger<OutboundMessageWorker> _logger;

    public OutboundMessageWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboundDispatchOptions> options,
        ILogger<OutboundMessageWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound message batch processing failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmppDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<ISmsGatewayClient>();

        var batch = await db.OutboundMessages
            .Where(m => m.Status == OutboundMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            return;
        }

        // Claim the batch up front so a second worker instance (e.g. during a rolling deploy)
        // won't pick up the same rows.
        foreach (var claimed in batch)
        {
            claimed.Status = OutboundMessageStatus.Sending;
        }
        await db.SaveChangesAsync(ct);

        foreach (var message in batch)
        {
            await SendOneAsync(db, gateway, message, ct);
            if (_options.DelayBetweenSendsMilliseconds > 0)
            {
                await Task.Delay(_options.DelayBetweenSendsMilliseconds, ct);
            }
        }
    }

    /// <summary>
    /// Writes exactly one History row per OutboundMessage, only once a terminal outcome is
    /// reached (sent, or failed after exhausting retries) - an earlier version wrote a History
    /// row on every attempt, which recorded a spurious "Failed" row for every retry that later
    /// succeeded.
    /// </summary>
    private async Task SendOneAsync(SmppDbContext db, ISmsGatewayClient gateway, OutboundMessage message, CancellationToken ct)
    {
        message.Attempts++;
        string? externalMessageId = null;
        string? gatewayResponse = null;
        bool succeeded;

        try
        {
            var result = await gateway.SendAsync(message.MessageText, message.ReceiverNumber, message.SenderNumber, ct);
            succeeded = result.Success;
            externalMessageId = result.ExternalMessageId;
            gatewayResponse = result.RawResponse;
            if (!succeeded)
            {
                message.LastError = "Gateway reported failure.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send failed for OutboundMessage {Id} (attempt {Attempt})", message.Id, message.Attempts);
            succeeded = false;
            message.LastError = ex.Message;
        }

        var exhausted = message.Attempts >= _options.MaxAttempts;
        if (succeeded || exhausted)
        {
            var history = new History
            {
                CampaignBatchId = message.CampaignBatchId,
                Source = message.Source,
                SenderNumber = message.SenderNumber,
                ReceiverNumber = message.ReceiverNumber,
                MessageText = message.MessageText,
                Status = succeeded ? MessageStatus.Sent : MessageStatus.Failed,
                ExternalMessageId = externalMessageId,
                GatewayResponse = gatewayResponse,
                CreatedByUserId = message.CreatedByUserId,
            };
            db.Histories.Add(history);
            await db.SaveChangesAsync(ct);

            message.HistoryId = history.Id;
            message.Status = succeeded ? OutboundMessageStatus.Sent : OutboundMessageStatus.Failed;
        }
        else
        {
            message.Status = OutboundMessageStatus.Pending;
        }

        await db.SaveChangesAsync(ct);
    }
}
