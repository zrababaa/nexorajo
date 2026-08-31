using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.LinkTracking;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

/// <summary>The destination URL to turn into a tracking link, and whether to shorten it.</summary>
public record PrepareLinkApiRequest
{
    [Required]
    public string Url { get; init; } = string.Empty;

    /// <summary>When true the <c>/l/{token}</c> redirect is run through Short.io before it comes back.</summary>
    public bool Shorten { get; init; }
}

/// <summary>
/// <see cref="TrackingUrl"/> is the string to drop into the message body. <see cref="Shortened"/>
/// is false when shortening was asked for but fell back to the full link (e.g. Short.io down).
/// </summary>
public record PrepareLinkApiResponse(string TrackingUrl, string DestinationUrl, bool Shortened);

/// <summary>Click stats for a send batch's tracking links - who clicked what, and when.</summary>
[Route("api/v1/link-tracking")]
[Tags("Link Tracking")]
public class LinkTrackingApiController : ApiControllerBase
{
    private readonly ILinkClickReportService _reports;
    private readonly ILinkTrackingService _tracking;

    public LinkTrackingApiController(ILinkClickReportService reports, ILinkTrackingService tracking)
    {
        _reports = reports;
        _tracking = tracking;
    }

    /// <summary>
    /// Mints a tracking link for a URL before it is sent, so the composer can put the real link
    /// (not the raw destination) into the message. The row is saved straightaway with no batch;
    /// the send whose message carries the link claims it.
    /// </summary>
    /// <response code="400">The URL was missing or not an absolute http(s) URL, or link tracking is not configured.</response>
    [HttpPost("prepare")]
    [ProducesResponseType(typeof(PrepareLinkApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Prepare([FromBody] PrepareLinkApiRequest request, CancellationToken ct)
    {
        var prepared = await _tracking.PrepareLinkAsync(request.Url, CurrentUserId, request.Shorten, ct);
        return Ok(new PrepareLinkApiResponse(prepared.TrackingUrl, prepared.DestinationUrl, prepared.Shortened));
    }

    /// <summary>Every tracking link visible to the caller, newest first. Superadmin sees all accounts' links.</summary>
    [HttpGet("links")]
    [ProducesResponseType(typeof(PagedResult<TrackedLinkRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Links([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _reports.GetLinksAsync(
            CurrentUserId, CurrentRole, Paging.Page(page), Paging.Size(pageSize), ct);
        return Ok(result);
    }

    /// <summary>Per-link summary for a batch: destination, click count, first/last click.</summary>
    [HttpGet("batches/{batchId}")]
    [ProducesResponseType(typeof(BatchLinkStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchStats(string batchId, CancellationToken ct)
    {
        var stats = await _reports.GetBatchStatsAsync(batchId, CurrentUserId, CurrentRole, ct);
        return stats is null ? NotFound(new ApiErrorResponse("No tracked links for this batch.")) : Ok(stats);
    }

    /// <summary>Individual clicks for a batch's tracking links, newest first.</summary>
    [HttpGet("batches/{batchId}/clicks")]
    [ProducesResponseType(typeof(PagedResult<LinkClickRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchClicks(
        string batchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _reports.GetBatchClicksAsync(
            batchId, CurrentUserId, CurrentRole, Paging.Page(page), Paging.Size(pageSize), ct);
        return result is null ? NotFound(new ApiErrorResponse("No tracked links for this batch.")) : Ok(result);
    }
}
