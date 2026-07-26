using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Unified per-recipient delivery log. Replaces legacy's separate historys /
/// api_historys / quick_send_history tables; Source distinguishes the origin.
/// </summary>
public class History : AuditableEntity, IHasCreator
{
    public string CampaignBatchId { get; set; } = string.Empty;
    public MessageSource Source { get; set; }
    public string SenderNumber { get; set; } = string.Empty;
    public string ReceiverNumber { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public MessageStatus Status { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? GatewayResponse { get; set; }
    public int CreatedByUserId { get; set; }
}
