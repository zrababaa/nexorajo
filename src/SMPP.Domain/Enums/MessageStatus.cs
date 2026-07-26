namespace SMPP.Domain.Enums;

/// <summary>
/// Normalizes legacy's free-text delivery statuses (DELIVRD/PROCESS/EXPIRED/SENT/UNDELIV).
/// </summary>
public enum MessageStatus
{
    Processing = 0,
    Sent = 1,
    Delivered = 2,
    Undelivered = 3,
    Expired = 4,
    Failed = 5
}
