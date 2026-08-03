using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Per-recipient delivery log, mapped onto the legacy <c>historys</c> table. Rows are written
/// by the external SMPP daemon as it drains <see cref="UnderProcess"/>, and its DLR callback
/// (see the getStatus endpoint) flips <see cref="Status"/> afterwards. This app only reads
/// them - nothing here inserts a History row - so the legacy column names and the string
/// status codes are the daemon's contract and must not be renamed.
/// </summary>
public class History : AuditableEntity, IHasCreator
{
    public string CampaignBatchId { get; set; } = string.Empty;
    public string? CampaignName { get; set; }
    public MessageSource Source { get; set; }
    public string SenderNumber { get; set; } = string.Empty;
    public string ReceiverNumber { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public MessageStatus Status { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? GatewayResponse { get; set; }
    public int CreatedByUserId { get; set; }
}
