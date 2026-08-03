using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SMPP.Domain.Enums;

namespace SMPP.Infrastructure.Persistence.Configurations;

/// <summary>
/// The text status codes the external SMPP daemon writes, shared by the two tables it logs to
/// (<c>historys</c> and <c>quick_send_history</c>). Stored as legacy text rather than enum
/// ordinals because the daemon's DLR callback writes them directly.
///
/// The read direction falls back to a default on an unrecognised code, so a daemon-side
/// vocabulary change degrades to a mislabelled row instead of a query that throws.
/// </summary>
internal static class LegacyMessageCodes
{
    public static readonly ValueConverter<MessageStatus, string> Status = new(
        status => status == MessageStatus.Delivered ? "DELIVRD"
            : status == MessageStatus.Sent ? "SENT"
            : status == MessageStatus.Undelivered ? "UNDELIV"
            : status == MessageStatus.Expired ? "EXPIRED"
            : status == MessageStatus.Failed ? "FAILED"
            : "PROCESS",
        code => code == "DELIVRD" ? MessageStatus.Delivered
            : code == "SENT" ? MessageStatus.Sent
            : code == "UNDELIV" ? MessageStatus.Undelivered
            : code == "EXPIRED" ? MessageStatus.Expired
            : code == "FAILED" ? MessageStatus.Failed
            : MessageStatus.Processing);

    public static readonly ValueConverter<MessageSource, string> Source = new(
        source => source == MessageSource.BulkSend ? "BTXTM"
            : source == MessageSource.PublicApi ? "ATXTM"
            : "STXTM",
        code => code == "BTXTM" ? MessageSource.BulkSend
            : code == "ATXTM" ? MessageSource.PublicApi
            : MessageSource.QuickSend);
}
