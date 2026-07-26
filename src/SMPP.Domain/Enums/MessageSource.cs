namespace SMPP.Domain.Enums;

/// <summary>
/// Which send flow produced a History/OutboundMessage row.
/// </summary>
public enum MessageSource
{
    QuickSend = 0,
    BulkSend = 1,
    PublicApi = 2
}
