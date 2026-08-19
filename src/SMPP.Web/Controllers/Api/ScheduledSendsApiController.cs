using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateScheduledSendApiRequest
{
    [Range(1, int.MaxValue)]
    public int CampaignId { get; init; }

    /// <summary>The literal message text. Ignored when <see cref="TemplateId"/> is set.</summary>
    public string? Message { get; init; }

    public string? SenderId { get; init; }

    /// <summary>Server local time. Must be in the future.</summary>
    [Required]
    public DateTime ScheduledAt { get; init; }

    /// <summary>Id of a saved SMS Template belonging to the caller, rendered per recipient at dispatch time instead of using <see cref="Message"/>.</summary>
    public int? TemplateId { get; init; }

    /// <summary>Values for the template's other placeholders (e.g. {"Date": "Friday 10am"}), same for every recipient. Required when the template uses any.</summary>
    public IReadOnlyDictionary<string, string>? TemplateVariables { get; init; }
}

public record ScheduledSendApiResponse(
    int Id,
    string CampaignName,
    string Message,
    string SenderId,
    DateTime ScheduledAtUtc,
    ScheduledSendStatus Status,
    string? BatchId,
    string? ErrorMessage,
    string? TemplateName);

/// <summary>
/// Bulk Sends scheduled to fire at a future exact time instead of immediately. Same pipeline as
/// an immediate Bulk Send (pricing, spam filter, sender-ID policy, sending window) - just deferred.
/// </summary>
[Route("api/v1/scheduled-sends")]
[Tags("Scheduled sends")]
public class ScheduledSendsApiController : ApiControllerBase
{
    private readonly IScheduledSendService _scheduledSends;

    public ScheduledSendsApiController(IScheduledSendService scheduledSends)
    {
        _scheduledSends = scheduledSends;
    }

    /// <summary>Schedules a Bulk Send. Rejected if the time is in the past or outside the admin sending window.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledSendApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateScheduledSendApiRequest request, CancellationToken ct)
    {
        var id = await _scheduledSends.CreateAsync(
            CurrentUserId,
            new CreateScheduledSendRequest(
                request.CampaignId, request.Message, request.SenderId ?? string.Empty, request.ScheduledAt, request.TemplateId, request.TemplateVariables),
            ct);

        var created = await _scheduledSends.GetByIdAsync(id, CurrentUserId, ct)
            ?? throw new AppException("Scheduled send not found.");

        return CreatedAtAction(nameof(GetAll), new { }, ToResponse(created));
    }

    /// <summary>Lists your own scheduled sends, most recently scheduled first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ScheduledSendApiResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _scheduledSends.GetPagedAsync(CurrentUserId, Paging.Page(page), Paging.Size(pageSize), ct);

        return Ok(new PagedResult<ScheduledSendApiResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
        });
    }

    /// <summary>Cancels a still-pending scheduled send.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _scheduledSends.CancelAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    private static ScheduledSendApiResponse ToResponse(ScheduledSendListItemDto dto) => new(
        dto.Id, dto.CampaignName, dto.Message, dto.SenderId, dto.ScheduledAtUtc, dto.Status, dto.BatchId, dto.ErrorMessage, dto.TemplateName);
}
