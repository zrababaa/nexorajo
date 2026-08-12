using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// A Bulk Send the owning user asked to fire at a future exact time instead of immediately.
/// <see cref="ScheduledAtUtc"/> is armed as a one-time Quartz trigger (see
/// ScheduledSendService/ScheduledSendDispatchJob in Infrastructure); this row is the durable
/// source of truth Quartz's in-memory job store is rebuilt from on every app restart.
/// </summary>
public class ScheduledSend : AuditableEntity, IHasCreator
{
    public int CampaignId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public ScheduledSendStatus Status { get; set; }
    public string? BatchId { get; set; }
    public string? ErrorMessage { get; set; }
    public int CreatedByUserId { get; set; }
}
