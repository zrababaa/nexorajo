using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Per-recipient delivery log for Quick Send, mapped onto the legacy <c>quick_send_history</c>
/// table.
///
/// Quick Send rows do not land in <see cref="History"/>: the external SMPP daemon splits its
/// output by the <c>push_type</c> it was handed, writing bulk batches to <c>historys</c> and
/// direct-number batches here, and the legacy DLR callback updates both tables by
/// <c>get_message_id</c>. So the Quick Send screen has to read this table or it shows nothing.
///
/// Like History, this app only reads these rows - nothing here inserts one - and the column
/// names are the daemon's contract.
/// </summary>
public class QuickSendHistory : AuditableEntity, IHasCreator
{
    public string CampaignBatchId { get; set; } = string.Empty;
    public string? CampaignName { get; set; }
    public string SenderNumber { get; set; } = string.Empty;
    public string ReceiverNumber { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public MessageStatus Status { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? GatewayResponse { get; set; }
    public int CreatedByUserId { get; set; }
}
