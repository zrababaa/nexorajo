namespace SMPP.Domain.Enums;

/// <summary>
/// Which send flow produced a History row. Persisted as the legacy message_type text code.
/// </summary>
public enum MessageSource
{
    QuickSend = 0,
    BulkSend = 1,
    PublicApi = 2
}
