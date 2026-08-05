using Microsoft.AspNetCore.Mvc;
using SMPP.Application.History;
using SMPP.Application.Reports;
using SMPP.Domain.Enums;
using SMPP.Web.Api;
using SMPP.Web.Services;

namespace SMPP.Web.Controllers.Api;

/// <summary>
/// The reporting surface behind the Reports tab - message detail, daily traffic, batches,
/// account usage, the balance ledger, and credit requests - each with the same filter set and
/// each exportable as CSV or Excel.
///
/// Scope is enforced server-side: an account only ever gets its own rows, whatever it asks for,
/// and the <c>accountId</c> filter is ignored for anyone but a Superadmin.
/// </summary>
[Route("api/v1/reports")]
[Tags("Reports")]
public class ReportsApiController : ApiControllerBase
{
    private readonly IReportService _reports;

    public ReportsApiController(IReportService reports)
    {
        _reports = reports;
    }

    /// <summary>Per-recipient message rows - the finest grain available.</summary>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryExportRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Messages([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var page = await _reports.GetMessagesAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>One row per calendar day: volume and delivery outcome.</summary>
    [HttpGet("daily-traffic")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyTrafficRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DailyTraffic([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var (page, _) = await _reports.GetDailyTrafficAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>One row per send batch: size, outcome, and cost.</summary>
    [HttpGet("batches")]
    [ProducesResponseType(typeof(IReadOnlyList<BatchReportRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Batches([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var (page, _) = await _reports.GetBatchesAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>One row per account: volume, delivery rate, credits in and out. An account calling it gets its own row.</summary>
    [HttpGet("account-usage")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountUsageRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AccountUsage([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var (page, _) = await _reports.GetAccountUsageAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>The balance ledger - every credit and debit.</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionReportRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transactions([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var (page, _) = await _reports.GetTransactionsAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>Submitted credit requests and how they were reviewed.</summary>
    [HttpGet("credit-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditRequestRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreditRequests([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var (page, _) = await _reports.GetCreditRequestsAsync(CurrentUserId, CurrentRole, Filter(query), PageNumber(query), PageSize(query), ct);
        return Ok(page.Items);
    }

    /// <summary>Any report as a downloadable file, with the same filters as its JSON endpoint.</summary>
    /// <param name="report">Which report to render.</param>
    /// <param name="format">"csv" (default) or "excel".</param>
    /// <param name="query">The shared report filter.</param>
    /// <param name="ct"></param>
    [HttpGet("{report}/export")]
    [Produces("text/csv", "application/vnd.ms-excel")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        ReportType report, [FromQuery] ReportQuery query, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        var table = await _reports.BuildExportAsync(report, CurrentUserId, CurrentRole, Filter(query), ct);

        return format.Equals("excel", StringComparison.OrdinalIgnoreCase)
            ? File(ReportExportWriter.WriteExcelHtml(table), ReportExportWriter.ExcelContentType, ReportExportWriter.FileName(table, "xls"))
            : File(ReportExportWriter.WriteCsv(table), ReportExportWriter.CsvContentType, ReportExportWriter.FileName(table, "csv"));
    }

    /// <summary>
    /// An account has no other account to look at, so its <c>accountId</c> is dropped rather
    /// than trusted - the same rule the Reports screen applies to its filter bar.
    /// </summary>
    private ReportFilterDto Filter(ReportQuery query) => new(
        query.DateFrom, query.DateTo, query.Source, query.Status, IsSuperadmin ? query.AccountId : null);

    /// <summary>
    /// Clamped so a caller that never heard of <see cref="ReportQuery.PageSize"/> still gets a
    /// bounded response instead of the old unbounded/near-unbounded dump.
    /// </summary>
    private static int PageSize(ReportQuery query) => Math.Clamp(query.PageSize, 1, MaxPageSize);

    private static int PageNumber(ReportQuery query) => Math.Max(1, query.Page);

    private const int MaxPageSize = 5_000;
}

/// <summary>
/// The filter every report takes. Not all of them honor every field - the message-grain reports
/// use <c>source</c>/<c>status</c>, the billing ones ignore them - but one shape keeps the
/// endpoints and their exports identical.
/// </summary>
public record ReportQuery
{
    /// <summary>Inclusive lower bound (UTC).</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>Inclusive upper bound (UTC).</summary>
    public DateOnly? DateTo { get; init; }

    public MessageSource? Source { get; init; }

    public MessageStatus? Status { get; init; }

    /// <summary>Restrict to one account. Superadmin only; ignored otherwise.</summary>
    public int? AccountId { get; init; }

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Rows per page, capped at 5,000.</summary>
    public int PageSize { get; init; } = 500;
}
