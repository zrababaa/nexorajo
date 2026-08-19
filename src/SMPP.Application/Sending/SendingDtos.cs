using SMPP.Domain.Enums;

namespace SMPP.Application.Sending;

public record SendSummaryDto(
    string BatchId,
    int RecipientCount,
    int SegmentsPerMessage,
    decimal TotalCost,
    decimal RemainingBalance);

public record QuickSendRequest(string RawNumbers, string Message, string SenderId);

/// <summary>
/// Exactly one of <paramref name="Message"/> or <paramref name="TemplateId"/> is used - a
/// template, when given, takes precedence over a raw <paramref name="Message"/>.
/// <paramref name="TemplateVariables"/> supplies values for the template's placeholders that
/// aren't resolved per-recipient from Customers (e.g. "Date"); see
/// <see cref="SMPP.Application.SmsTemplates.TemplatePlaceholders"/>.
/// </summary>
public record BulkSendRequest(
    int CampaignId,
    string? Message,
    string SenderId,
    int? TemplateId = null,
    IReadOnlyDictionary<string, string>? TemplateVariables = null);

/// <summary>See <see cref="BulkSendRequest"/> for how <paramref name="TemplateId"/>/<paramref name="TemplateVariables"/> interact with <paramref name="Message"/>.</summary>
public record CreateScheduledSendRequest(
    int CampaignId,
    string? Message,
    string SenderId,
    DateTime ScheduledAtLocal,
    int? TemplateId = null,
    IReadOnlyDictionary<string, string>? TemplateVariables = null);

public record ScheduledSendListItemDto(
    int Id,
    string CampaignName,
    string Message,
    string SenderId,
    DateTime ScheduledAtUtc,
    ScheduledSendStatus Status,
    string? BatchId,
    string? ErrorMessage,
    string? TemplateName = null);
