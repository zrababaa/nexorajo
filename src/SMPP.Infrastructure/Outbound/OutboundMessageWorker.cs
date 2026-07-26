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
/// rows, claims a batch, sends each through IWhatsAppGatewayClient, and writes one History row
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
        var gateway = scope.ServiceProvider.GetRequiredService<IWhatsAppGatewayClient>();

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

    private async Task SendOneAsync(SmppDbContext db, IWhatsAppGatewayClient gateway, OutboundMessage message, CancellationToken ct)
    {
        message.Attempts++;

        try
        {
            var result = await gateway.SendAsync(message.MessageText, message.ReceiverNumber, message.SenderNumber, ct);

            var history = new History
            {
                CampaignBatchId = message.CampaignBatchId,
                Source = message.Source,
                SenderNumber = message.SenderNumber,
                ReceiverNumber = message.ReceiverNumber,
                MessageText = message.MessageText,
                Status = result.Success ? MessageStatus.Sent : MessageStatus.Failed,
                ExternalMessageId = result.ExternalMessageId,
                GatewayResponse = result.RawResponse,
                CreatedByUserId = message.CreatedByUserId,
            };
            db.Histories.Add(history);
            await db.SaveChangesAsync(ct);

            message.HistoryId = history.Id;

            if (result.Success)
            {
                message.Status = OutboundMessageStatus.Sent;
            }
            else
            {
                message.LastError = "Gateway reported failure.";
                message.Status = message.Attempts >= _options.MaxAttempts
                    ? OutboundMessageStatus.Failed
                    : OutboundMessageStatus.Pending;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send failed for OutboundMessage {Id} (attempt {Attempt})", message.Id, message.Attempts);
            message.LastError = ex.Message;
            message.Status = message.Attempts >= _options.MaxAttempts
                ? OutboundMessageStatus.Failed
                : OutboundMessageStatus.Pending;
        }

        await db.SaveChangesAsync(ct);
    }
}
