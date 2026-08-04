using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.Reports;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Services;
using SMPP.Web.ViewModels.AccountsLogs;
using SMPP.Web.ViewModels.Reports;

namespace SMPP.Web.Controllers;

/// <summary>
/// Superadmin-only: every bulk-send batch across every account - the same per-batch data as
/// Reports &gt; Batches, pinned to the Bulk Send channel.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin)]
public class AccountsLogsController : Controller
{
    private readonly IReportService _reports;
    private readonly IAccountService _accounts;
    private readonly ICurrentUserService _currentUser;

    public AccountsLogsController(IReportService reports, IAccountService accounts, ICurrentUserService currentUser)
    {
        _reports = reports;
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(DateOnly? dateFrom = null, DateOnly? dateTo = null, int? accountId = null, CancellationToken ct = default)
    {
        var model = new AccountsLogsViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            AccountId = accountId,
            Accounts = (await _accounts.GetOptionsAsync(ct)).Select(a => new AccountOption(a.Id, a.Username)).ToList(),
            Batches = await _reports.GetBatchesAsync(_currentUser.UserId, _currentUser.Role, ToFilter(dateFrom, dateTo, accountId), ct),
        };

        return View(model);
    }

    public Task<IActionResult> ExportExcel(DateOnly? dateFrom = null, DateOnly? dateTo = null, int? accountId = null, CancellationToken ct = default) =>
        ExportAsync(dateFrom, dateTo, accountId, ReportExportWriter.WriteExcelHtml, ReportExportWriter.ExcelContentType, "xls", ct);

    public Task<IActionResult> ExportCsv(DateOnly? dateFrom = null, DateOnly? dateTo = null, int? accountId = null, CancellationToken ct = default) =>
        ExportAsync(dateFrom, dateTo, accountId, ReportExportWriter.WriteCsv, ReportExportWriter.CsvContentType, "csv", ct);

    private async Task<IActionResult> ExportAsync(
        DateOnly? dateFrom, DateOnly? dateTo, int? accountId,
        Func<ReportTable, byte[]> write, string contentType, string extension, CancellationToken ct)
    {
        var table = await _reports.BuildExportAsync(
            ReportType.Batches, _currentUser.UserId, _currentUser.Role, ToFilter(dateFrom, dateTo, accountId), ct);

        return File(write(table), contentType, ReportExportWriter.FileName(table, extension));
    }

    /// <summary>Always pinned to Bulk Send - this page is the bulk-operations log, not the general batch report.</summary>
    private static ReportFilterDto ToFilter(DateOnly? dateFrom, DateOnly? dateTo, int? accountId) =>
        new(dateFrom, dateTo, MessageSource.BulkSend, null, accountId);
}
