using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Sending;
using SMPP.Domain.Pricing;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record QuickSendApiRequest
{
    /// <summary>Recipient numbers, separated by commas, spaces, or new lines. Deduplicated before sending.</summary>
    [Required]
    public string Numbers { get; init; } = string.Empty;

    [Required]
    public string Message { get; init; } = string.Empty;

    /// <summary>Leave empty to send under the account's first assigned Sender ID.</summary>
    public string? SenderId { get; init; }
}

public record BulkSendApiRequest
{
    /// <summary>Id of a saved recipient list belonging to the caller.</summary>
    [Range(1, int.MaxValue)]
    public int CampaignId { get; init; }

    [Required]
    public string Message { get; init; } = string.Empty;

    public string? SenderId { get; init; }
}

public record SendPolicyApiResponse(
    bool AllowFreeSenderId,
    IReadOnlyList<string> AllowedSenderIds,
    decimal CreditsPerMessagePart);

public record MessagePreviewApiRequest
{
    [Required]
    public string Message { get; init; } = string.Empty;

    /// <summary>How many recipients to price the message for. Defaults to one.</summary>
    [Range(1, int.MaxValue)]
    public int RecipientCount { get; init; } = 1;
}

public record MessagePreviewApiResponse(
    int Characters,
    int Segments,
    int RecipientCount,
    decimal CreditsPerMessagePart,
    decimal TotalCost);

/// <summary>
/// Sending. Both endpoints run the same pipeline as the web UI - content filter, Sender ID
/// policy, segment count, balance debit - and hand the batch to the SMPP daemon, which delivers
/// it and reports each recipient's outcome back into History.
///
/// The response is an acknowledgement that the batch was accepted and charged, not a delivery
/// confirmation: poll <c>/api/v1/history?campaignBatchId={batchId}</c> for per-recipient status.
/// </summary>
[Route("api/v1/messages")]
[Tags("Messaging")]
public class MessagesApiController : ApiControllerBase
{
    private readonly IQuickSendService _quickSend;
    private readonly IBulkSendService _bulkSend;
    private readonly ISendPolicyService _sendPolicy;
    private readonly ISegmentCounter _segmentCounter;

    public MessagesApiController(
        IQuickSendService quickSend,
        IBulkSendService bulkSend,
        ISendPolicyService sendPolicy,
        ISegmentCounter segmentCounter)
    {
        _quickSend = quickSend;
        _bulkSend = bulkSend;
        _sendPolicy = sendPolicy;
        _segmentCounter = segmentCounter;
    }

    /// <summary>What the account may send under, and what a message part costs it.</summary>
    [HttpGet("send-policy")]
    [ProducesResponseType(typeof(SendPolicyApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSendPolicy(CancellationToken ct)
    {
        var policy = await _sendPolicy.GetAsync(CurrentUserId, ct);
        return Ok(new SendPolicyApiResponse(policy.AllowFreeSenderId, policy.AllowedSenderIds, policy.CreditsPerMessagePart));
    }

    /// <summary>
    /// Prices a message without sending it: how many parts it splits into and what it would cost
    /// for the given number of recipients. Uses the same segment counter as the send path.
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(MessagePreviewApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromBody] MessagePreviewApiRequest request, CancellationToken ct)
    {
        var policy = await _sendPolicy.GetAsync(CurrentUserId, ct);
        var segments = _segmentCounter.CountSegments(request.Message);

        return Ok(new MessagePreviewApiResponse(
            request.Message.Length,
            segments,
            request.RecipientCount,
            policy.CreditsPerMessagePart,
            request.RecipientCount * segments * policy.CreditsPerMessagePart));
    }

    /// <summary>Sends a message to numbers supplied inline.</summary>
    /// <response code="422">The content filter blocked the message; nothing was charged or queued.</response>
    [HttpPost("quick-send")]
    [ProducesResponseType(typeof(SendSummaryDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> QuickSend([FromBody] QuickSendApiRequest request, CancellationToken ct)
    {
        var summary = await _quickSend.SubmitAsync(
            CurrentUserId, new QuickSendRequest(request.Numbers, request.Message, request.SenderId ?? string.Empty), ct);

        return Accepted(summary);
    }

    /// <summary>Sends a message to every number in one of the caller's saved lists.</summary>
    /// <response code="422">The content filter blocked the message; nothing was charged or queued.</response>
    [HttpPost("bulk-send")]
    [ProducesResponseType(typeof(SendSummaryDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BulkSend([FromBody] BulkSendApiRequest request, CancellationToken ct)
    {
        var summary = await _bulkSend.SubmitAsync(
            CurrentUserId, new BulkSendRequest(request.CampaignId, request.Message, request.SenderId ?? string.Empty), ct);

        return Accepted(summary);
    }

    /// <summary>The flat price of one message part for one recipient, before any account policy.</summary>
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public IActionResult GetPricing() => Ok(MessagePricing.CreditsPerMessagePart);
}
