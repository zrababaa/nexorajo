using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Single outbound-send queue table, replacing legacy's four overlapping/mostly-dead
/// mechanisms (under_process, synchronous curl, a never-dispatched queued job, and a
/// scheduled closure/Artisan command pair reading a table nothing ever inserted into).
/// Polled by OutboundMessageWorker.
/// </summary>
public class OutboundMessage : AuditableEntity, IHasCreator
{
    public string CampaignBatchId { get; set; } = string.Empty;
    public MessageSource Source { get; set; }
    public string SenderNumber { get; set; } = string.Empty;
    public string ReceiverNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public OutboundMessageStatus Status { get; set; } = OutboundMessageStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public int? HistoryId { get; set; }
}
