using SMPP.Domain.Enums;

namespace SMPP.Application.Sending;

public record SendSummaryDto(
    string BatchId,
    int RecipientCount,
    int SegmentsPerMessage,
    decimal TotalCost,
    decimal RemainingBalance);

public record QuickSendRequest(string RawNumbers, string Message, string SenderId);

public record BulkSendRequest(int CampaignId, string Message, string SenderId);

public record CreateScheduledSendRequest(int CampaignId, string Message, string SenderId, DateTime ScheduledAtLocal);

public record ScheduledSendListItemDto(
    int Id,
    string CampaignName,
    string Message,
    string SenderId,
    DateTime ScheduledAtUtc,
    ScheduledSendStatus Status,
    string? BatchId,
    string? ErrorMessage);
