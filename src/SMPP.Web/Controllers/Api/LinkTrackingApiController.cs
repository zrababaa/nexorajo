using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.LinkTracking;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

/// <summary>Click stats for a send batch's tracking links - who clicked what, and when.</summary>
[Route("api/v1/link-tracking")]
[Tags("Link Tracking")]
public class LinkTrackingApiController : ApiControllerBase
{
    private readonly ILinkClickReportService _reports;

    public LinkTrackingApiController(ILinkClickReportService reports)
    {
        _reports = reports;
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
