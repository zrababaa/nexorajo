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

    /// <summary>
    /// The literal text to send, or - when <see cref="TemplateId"/> is set - a display-only
    /// preview of the template (global variables filled in, per-recipient placeholders like
    /// "[Name]" left as-is). The actual send always re-renders per recipient from
    /// <see cref="TemplateId"/>/<see cref="TemplateVariablesJson"/> at dispatch time, using
    /// whatever Customer data exists then; this field is never read for that.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public ScheduledSendStatus Status { get; set; }
    public string? BatchId { get; set; }
    public string? ErrorMessage { get; set; }
    public int CreatedByUserId { get; set; }

    /// <summary>Set instead of a plain message when this send was created from an SMS Template. No FK - same convention as <see cref="CampaignId"/>.</summary>
    public int? TemplateId { get; set; }

    /// <summary>JSON-serialized global template variable values (e.g. {"Date":"Friday"}), used to re-render the template at dispatch time.</summary>
    public string? TemplateVariablesJson { get; set; }
}
