using SMPP.Application.Common;

namespace SMPP.Application.Sending;

/// <summary>
/// User-scheduled Bulk Sends: fires the same debit+filter+enqueue pipeline as an immediate Bulk
/// Send (SendCore, via ScheduledSendDispatchJob), just at a future exact time chosen by the
/// caller instead of right away.
/// </summary>
public interface IScheduledSendService
{
    /// <exception cref="AppException">
    /// Thrown when the campaign is not owned by <paramref name="userId"/>, the scheduled time is
    /// not in the future, or it falls in a time of day the admin sending window would block.
    /// </exception>
    Task<int> CreateAsync(int userId, CreateScheduledSendRequest request, CancellationToken ct = default);

    Task<ScheduledSendListItemDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);

    Task<PagedResult<ScheduledSendListItemDto>> GetPagedAsync(int userId, int page, int pageSize, CancellationToken ct = default);

    /// <exception cref="AppException">Thrown when not found, not owned by the caller, or no longer Pending.</exception>
    Task CancelAsync(int id, int userId, CancellationToken ct = default);
}
