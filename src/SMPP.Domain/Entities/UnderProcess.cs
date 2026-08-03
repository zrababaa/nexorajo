using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Hand-off queue to the external SMPP daemon, mapped onto the legacy <c>under_process</c>
/// table. One row per submission - not per recipient - holding the whole recipient list in
/// <see cref="CampaignNumbers"/> as a comma-separated string, exactly as legacy's
/// Whatsapp::submitQuickSend / submitBulkSend / SendMessageApi::sendMessage wrote it.
///
/// This app only ever INSERTs here. The daemon polls the table, does the actual SMPP
/// submission, and writes the per-recipient <see cref="History"/> rows back. Column names and
/// the push_type/priority integers belong to that daemon, so they must stay as-is.
/// </summary>
public class UnderProcess : AuditableEntity
{
    public string CampaignId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CampaignNumbers { get; set; } = string.Empty;
    public string? CampaignName { get; set; }
    public PushType PushType { get; set; }
    public int UserId { get; set; }
    public SendPriority Priority { get; set; }
    public string? FilePath { get; set; }
}
