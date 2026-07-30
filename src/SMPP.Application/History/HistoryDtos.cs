using SMPP.Domain.Enums;

namespace SMPP.Application.History;

public record HistoryListItemDto(
    int Id,
    string CampaignBatchId,
    MessageSource Source,
    string SenderNumber,
    string ReceiverNumber,
    MessageStatus Status,
    DateTime CreatedAt,
    int CreatedByUserId);

public record HistorySummaryDto(
    int Total,
    int Delivered,
    int Sent,
    int Processing,
    int Undelivered,
    int Failed,
    int Expired);

/// <summary>
/// Shared filter set for the History tab's list, summary, and export queries so all three
/// stay consistent - a user exporting "what they're currently looking at" gets exactly that.
/// </summary>
public record HistoryFilterDto(
    MessageSource? Source = null,
    MessageStatus? Status = null,
    string? CampaignBatchId = null,
    string? ReceiverSearch = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);

public record HistoryExportRowDto(
    int Id,
    string CampaignBatchId,
    MessageSource Source,
    string SenderNumber,
    string ReceiverNumber,
    string? MessageText,
    MessageStatus Status,
    string? ExternalMessageId,
    DateTime CreatedAt);
