using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.SendingWindow;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class SendingWindowService : ISendingWindowService
{
    private const int SingletonId = 1;

    private readonly SmppDbContext _db;

    public SendingWindowService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<SendingWindowDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        return ToDto(settings);
    }

    public async Task<SendingWindowDto> SetAsync(int performedByUserId, bool isEnabled, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default)
    {
        var settings = await _db.SendingWindowSettings.SingleAsync(s => s.Id == SingletonId, ct);
        settings.IsEnabled = isEnabled;
        settings.StartTime = startTime;
        settings.EndTime = endTime;

        await _db.SaveChangesAsync(ct);

        return ToDto(settings);
    }

    public async Task<bool> IsTimeOfDayAllowedAsync(TimeOnly timeOfDay, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        return !settings.IsEnabled || IsWithinWindow(timeOfDay, settings.StartTime, settings.EndTime);
    }

    public async Task EnsureBulkSendAllowedNowAsync(CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        var now = TimeOnly.FromDateTime(DateTime.Now);

        if (settings.IsEnabled && !IsWithinWindow(now, settings.StartTime, settings.EndTime))
        {
            throw new AppException(
                $"Bulk sending is currently blocked. It is only allowed between {settings.StartTime:HH:mm} and {settings.EndTime:HH:mm}.");
        }
    }

    /// <summary>Handles a window that wraps past midnight (e.g. 21:00-09:00), where start &gt; end.</summary>
    private static bool IsWithinWindow(TimeOnly now, TimeOnly start, TimeOnly end) =>
        start <= end ? now >= start && now < end : now >= start || now < end;

    private async Task<SendingWindowSettings> GetSettingsAsync(CancellationToken ct) =>
        await _db.SendingWindowSettings.AsNoTracking().SingleAsync(s => s.Id == SingletonId, ct);

    private static SendingWindowDto ToDto(SendingWindowSettings settings) =>
        new(settings.IsEnabled, settings.StartTime, settings.EndTime);
}
