using SMPP.Domain.Enums;

namespace SMPP.Application.History;

public record HistoryListItemDto(
    int Id,
    string CampaignBatchId,
    MessageSource Source,
    string SenderNumber,
    string ReceiverNumber,
    MessageStatus Status,
    DateTime CreatedAt);

public record HistorySummaryDto(
    int Total,
    int Delivered,
    int Sent,
    int Processing,
    int Undelivered,
    int Failed,
    int Expired);
