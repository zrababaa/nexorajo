using SMPP.Domain.Enums;

namespace SMPP.Application.Campaigns;

public record CampaignListItemDto(
    int Id,
    string Name,
    string ExternalCampaignCode,
    int RecipientCount,
    CampaignSourceType SourceType,
    DateTime CreatedAt,
    IReadOnlyList<string>? ImportedColumns);

public record CampaignDetailDto(
    int Id,
    string Name,
    string ExternalCampaignCode,
    string Numbers,
    int RecipientCount,
    CampaignSourceType SourceType,
    IReadOnlyList<string>? ImportedColumns);

/// <summary>
/// <paramref name="RecipientVariablesJson"/>/<paramref name="ImportedColumns"/> come from
/// <c>NumberListResult.RowVariablesByNumber</c>/<c>Columns</c> on a headered CSV/XLSX import -
/// null for numbers pasted or imported without a header row.
/// </summary>
public record CreateCampaignRequest(
    string Name,
    string ExternalCampaignCode,
    string NormalizedNumbers,
    CampaignSourceType SourceType,
    string? RecipientVariablesJson = null,
    IReadOnlyList<string>? ImportedColumns = null);

public record UpdateCampaignRequest(
    string Name,
    string NormalizedNumbers);
