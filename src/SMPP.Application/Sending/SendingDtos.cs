namespace SMPP.Application.Sending;

public record SendSummaryDto(
    string BatchId,
    int RecipientCount,
    int SegmentsPerMessage,
    decimal TotalCost,
    decimal RemainingBalance);

public record QuickSendRequest(string RawNumbers, string Message, string SenderId);

public record BulkSendRequest(int CampaignId, string Message, string SenderId);
