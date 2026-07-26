namespace SMPP.Domain.Enums;

/// <summary>
/// Which of the (unified) send flows produced a History/OutboundMessage row.
/// Replaces the legacy split between historys / api_historys / quick_send_history tables.
/// </summary>
public enum MessageSource
{
    QuickSend = 0,
    BulkSend = 1,
    BulkTemplateSend = 2,
    PublicApi = 3
}
