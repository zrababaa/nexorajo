using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// A saved recipient list (legacy called this a "campaign", but it is not a marketing campaign).
/// Always a flat, deduplicated comma-separated number list regardless of how it was entered -
/// legacy's per-recipient "names" CSV column exists on the table but is never actually
/// populated by the create flow, so it isn't carried forward here.
/// </summary>
public class Campaign : AuditableEntity, IHasCreator
{
    public string Name { get; set; } = string.Empty;
    public string ExternalCampaignCode { get; set; } = string.Empty;
    public string Numbers { get; set; } = string.Empty;
    public CampaignSourceType SourceType { get; set; }
    public int? SendSpeedMinSeconds { get; set; }
    public int? SendSpeedMaxSeconds { get; set; }
    public int CreatedByUserId { get; set; }
}
