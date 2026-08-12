using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// One row per visit to a tracking link. BatchId is denormalized from TrackedLink (not just
/// reachable via TrackedLinkId) so "list clicks for batch X" is a single indexed query, the
/// same shape History.CampaignBatchId already uses instead of joining back through UnderProcess.
/// </summary>
public class LinkClick : AuditableEntity
{
    public int TrackedLinkId { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public DateTime ClickedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}
