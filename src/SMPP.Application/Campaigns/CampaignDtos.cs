using SMPP.Domain.Enums;

namespace SMPP.Application.Campaigns;

public record CampaignListItemDto(
    int Id,
    string Name,
    string ExternalCampaignCode,
    int RecipientCount,
    CampaignSourceType SourceType,
    DateTime CreatedAt);

public record CampaignDetailDto(
    int Id,
    string Name,
    string ExternalCampaignCode,
    string Numbers,
    int RecipientCount,
    CampaignSourceType SourceType);

public record CreateCampaignRequest(
    string Name,
    string ExternalCampaignCode,
    string NormalizedNumbers,
    CampaignSourceType SourceType);

public record UpdateCampaignRequest(
    string Name,
    string NormalizedNumbers);
