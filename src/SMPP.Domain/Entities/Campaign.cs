using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// A saved recipient list (legacy called this a "campaign", but it is not a marketing campaign).
/// Always a flat, deduplicated comma-separated number list regardless of how it was entered.
/// When imported from a file with a header row (e.g. Phone, Name, Amount), the non-phone columns
/// are kept as per-recipient template variables in <see cref="RecipientVariablesJson"/>/
/// <see cref="ImportedColumnsJson"/> - see TemplateMessageResolver.
/// </summary>
public class Campaign : AuditableEntity, IHasCreator
{
    public string Name { get; set; } = string.Empty;
    public string ExternalCampaignCode { get; set; } = string.Empty;
    public string Numbers { get; set; } = string.Empty;
    public CampaignSourceType SourceType { get; set; }
    public int CreatedByUserId { get; set; }

    /// <summary>JSON <c>{ number: { column: value } }</c> from a headered file import; null otherwise.</summary>
    public string? RecipientVariablesJson { get; set; }

    /// <summary>JSON string array of the non-phone column names available in <see cref="RecipientVariablesJson"/>; null otherwise.</summary>
    public string? ImportedColumnsJson { get; set; }
}
