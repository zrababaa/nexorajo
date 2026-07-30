using SMPP.Domain.Enums;

namespace SMPP.Web.Services;

/// <summary>
/// Shared color/label mapping for MessageStatus and MessageSource so the Dashboard and
/// History charts render the same status consistently in one color everywhere.
/// </summary>
public static class ChartColors
{
    public static string StatusColor(MessageStatus status) => status switch
    {
        MessageStatus.Delivered => "#0ca30c",
        MessageStatus.Sent => "#2a78d6",
        MessageStatus.Processing => "#fab219",
        MessageStatus.Expired => "#4a3aa7",
        MessageStatus.Undelivered => "#ec835a",
        MessageStatus.Failed => "#d03b3b",
        _ => "#898781",
    };

    public static string SourceColor(MessageSource source) => source switch
    {
        MessageSource.QuickSend => "#2a78d6",
        MessageSource.BulkSend => "#eb6834",
        MessageSource.PublicApi => "#1baf7a",
        _ => "#898781",
    };

    public static string SourceLabel(MessageSource source) => source switch
    {
        MessageSource.QuickSend => "Quick Send",
        MessageSource.BulkSend => "Bulk Send",
        MessageSource.PublicApi => "Public API",
        _ => source.ToString(),
    };
}
