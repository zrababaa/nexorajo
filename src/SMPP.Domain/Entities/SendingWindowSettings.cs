namespace SMPP.Domain.Entities;

/// <summary>
/// The daily time-of-day window Bulk Send is allowed in. Singleton row (Id = 1). When
/// <see cref="IsEnabled"/> is false, Bulk Send is unrestricted regardless of the times below -
/// this is the default so adding the feature does not change behavior until a Superadmin opts in.
/// <see cref="StartTime"/>/<see cref="EndTime"/> are server local time; a window where
/// Start &gt; End wraps past midnight (e.g. 21:00-09:00).
/// </summary>
public class SendingWindowSettings
{
    public int Id { get; set; }
    public bool IsEnabled { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
