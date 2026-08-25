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

/// <summary>One imported row: <paramref name="Number"/> is the normalized recipient number, <paramref name="Values"/> is every column's raw value for that row (see <see cref="ICampaignNumberParser"/>).</summary>
public record CampaignRecipientRowDto(string Number, IReadOnlyDictionary<string, string> Values);

/// <summary><paramref name="Recipients"/> is populated in the same order as <paramref name="Numbers"/> whenever the campaign came from a headered file import; null otherwise (pasted numbers, or a file with no header row).</summary>
public record CampaignDetailDto(
    int Id,
    string Name,
    string ExternalCampaignCode,
    string Numbers,
    int RecipientCount,
    CampaignSourceType SourceType,
    IReadOnlyList<string>? ImportedColumns,
    IReadOnlyList<CampaignRecipientRowDto>? Recipients);

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
