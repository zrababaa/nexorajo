using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Web.Api;
using SMPP.Web.Services;

namespace SMPP.Web.Controllers.Api;

/// <summary>
/// The filter every history endpoint takes, so the list, the counts, and the export always
/// describe the same set of messages.
/// </summary>
public record HistoryQuery
{
    /// <summary>Restrict to Quick Send, Bulk Send, or public-API traffic.</summary>
    public MessageSource? Source { get; init; }

    /// <summary>Restrict to one delivery status.</summary>
    public MessageStatus? Status { get; init; }

    /// <summary>The batch id returned by a send call.</summary>
    public string? CampaignBatchId { get; init; }

    /// <summary>Partial recipient-number match.</summary>
    public string? Receiver { get; init; }

    /// <summary>Inclusive lower bound on the send date (UTC).</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>Inclusive upper bound on the send date (UTC).</summary>
    public DateOnly? DateTo { get; init; }

    public HistoryFilterDto ToFilter() => new(Source, Status, CampaignBatchId, Receiver, DateFrom, DateTo);
}

/// <summary>
/// Message history across Quick Send, Bulk Send, and the public API, scoped by role: an account
/// sees only its own rows, a Superadmin sees everyone's. This is where a client polls for the
/// per-recipient outcome of a batch it sent.
/// </summary>
[Route("api/v1/history")]
[Tags("History")]
public class HistoryApiController : ApiControllerBase
{
    private readonly IHistoryService _history;
    private readonly IHistoryResendService _resend;

    public HistoryApiController(IHistoryService history, IHistoryResendService resend)
    {
        _history = history;
        _resend = resend;
    }

    /// <summary>Lists message rows, newest first.</summary>
    /// <param name="query">Filters; every field is optional.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page, capped at 200.</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HistoryListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] HistoryQuery query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _history.GetPagedAsync(CurrentUserId, CurrentRole, query.ToFilter(), Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>Counts per delivery status for the same filters the list endpoint takes.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(HistorySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] HistoryQuery query, CancellationToken ct) =>
        Ok(await _history.GetSummaryAsync(CurrentUserId, CurrentRole, query.ToFilter(), ct));

    /// <summary>
    /// The filtered rows as JSON with the message body included - the same set the export
    /// endpoint writes, for a client that would rather have the data than a file.
    /// </summary>
    [HttpGet("rows")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryExportRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRows([FromQuery] HistoryQuery query, CancellationToken ct) =>
        Ok(await _history.GetForExportAsync(CurrentUserId, CurrentRole, query.ToFilter(), ct));

    /// <summary>Downloads the filtered rows as a file.</summary>
    /// <param name="query">Filters; every field is optional.</param>
    /// <param name="format">"csv" (default) or "excel".</param>
    /// <param name="ct"></param>
    [HttpGet("export")]
    [Produces("text/csv", "application/vnd.ms-excel")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] HistoryQuery query, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        var rows = await _history.GetForExportAsync(CurrentUserId, CurrentRole, query.ToFilter(), ct);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        return format.Equals("excel", StringComparison.OrdinalIgnoreCase)
            ? File(HistoryExportWriter.WriteExcelHtml(rows), "application/vnd.ms-excel", $"history-{stamp}.xls")
            : File(HistoryExportWriter.WriteCsv(rows), "text/csv", $"history-{stamp}.csv");
    }

    /// <summary>
    /// Re-queues one message that did not get through (Failed, Undelivered, or Expired) as a new
    /// single-recipient batch. It is charged again like any other send.
    /// </summary>
    /// <param name="id">History row id.</param>
    /// <param name="source">Which history the id belongs to - ids are only unique within a source.</param>
    /// <param name="ct"></param>
    [HttpPost("{id:int}/resend")]
    [ProducesResponseType(typeof(SendSummaryDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Resend(int id, [FromQuery] MessageSource source, CancellationToken ct) =>
        Accepted(await _resend.ResendAsync(id, source, CurrentUserId, ct));
}
