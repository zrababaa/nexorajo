namespace SMPP.Application.SendingWindow;

public record SendingWindowDto(bool IsEnabled, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>
/// The Superadmin-controlled daily time-of-day window Bulk Send is restricted to. Enforced once,
/// in SendCore, so Quick Send and the public API are never affected - only Bulk Send (immediate or
/// scheduled) is gated on it.
/// </summary>
public interface ISendingWindowService
{
    Task<SendingWindowDto> GetAsync(CancellationToken ct = default);

    Task<SendingWindowDto> SetAsync(int performedByUserId, bool isEnabled, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default);

    Task<bool> IsTimeOfDayAllowedAsync(TimeOnly timeOfDay, CancellationToken ct = default);

    /// <exception cref="SMPP.Application.Common.AppException">Thrown when Bulk Send is blocked right now.</exception>
    Task EnsureBulkSendAllowedNowAsync(CancellationToken ct = default);
}
