using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.Reports;
using SMPP.Domain.Enums;
using SMPP.Web.Services;
using SMPP.Web.ViewModels.Reports;

namespace SMPP.Web.Controllers;

/// <summary>
/// One page, one filter bar, several reports - message detail, daily traffic, batches, account
/// usage, the balance ledger, and credit requests - each exportable to Excel or CSV.
///
/// The report is a route value rather than an action per report so the filter, the tab strip,
/// and the export links are written once. Scoping is IReportService's job: an Account only ever
/// gets its own rows whatever it asks for.
/// </summary>
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly IAccountService _accounts;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IReportService reports, IAccountService accounts, ICurrentUserService currentUser)
    {
        _reports = reports;
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        ReportType report = ReportType.Messages,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        MessageSource? source = null, MessageStatus? status = null, int? accountId = null,
        CancellationToken ct = default)
    {
        var filter = await BuildFilterAsync(report, dateFrom, dateTo, source, status, accountId, ct);
        var model = new ReportsViewModel { Filter = filter };

        var userId = _currentUser.UserId;
        var role = _currentUser.Role;
        var dto = filter.ToFilter();

        switch (report)
        {
            case ReportType.DailyTraffic:
                model.DailyTraffic = await _reports.GetDailyTrafficAsync(userId, role, dto, ct);
                break;
            case ReportType.Batches:
                model.Batches = await _reports.GetBatchesAsync(userId, role, dto, ct);
                break;
            case ReportType.AccountUsage:
                model.AccountUsage = await _reports.GetAccountUsageAsync(userId, role, dto, ct);
                break;
            case ReportType.Transactions:
                model.Transactions = await _reports.GetTransactionsAsync(userId, role, dto, ct);
                break;
            case ReportType.CreditRequests:
                model.CreditRequests = await _reports.GetCreditRequestsAsync(userId, role, dto, ct);
                break;
            default:
                model.Messages = await _reports.GetMessagesAsync(userId, role, dto, ct);
                break;
        }

        return View(model);
    }

    public Task<IActionResult> ExportExcel(
        ReportType report = ReportType.Messages,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        MessageSource? source = null, MessageStatus? status = null, int? accountId = null,
        CancellationToken ct = default) =>
        ExportAsync(report, new ReportFilterDto(dateFrom, dateTo, source, status, accountId),
            ReportExportWriter.WriteExcelHtml, ReportExportWriter.ExcelContentType, "xls", ct);

    public Task<IActionResult> ExportCsv(
        ReportType report = ReportType.Messages,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        MessageSource? source = null, MessageStatus? status = null, int? accountId = null,
        CancellationToken ct = default) =>
        ExportAsync(report, new ReportFilterDto(dateFrom, dateTo, source, status, accountId),
            ReportExportWriter.WriteCsv, ReportExportWriter.CsvContentType, "csv", ct);

    private async Task<IActionResult> ExportAsync(
        ReportType report, ReportFilterDto filter,
        Func<ReportTable, byte[]> write, string contentType, string extension, CancellationToken ct)
    {
        var table = await _reports.BuildExportAsync(report, _currentUser.UserId, _currentUser.Role, filter, ct);
        return File(write(table), contentType, ReportExportWriter.FileName(table, extension));
    }

    private async Task<ReportFilterViewModel> BuildFilterAsync(
        ReportType report, DateOnly? dateFrom, DateOnly? dateTo,
        MessageSource? source, MessageStatus? status, int? accountId, CancellationToken ct)
    {
        var isSuperadmin = _currentUser.Role == UserRole.Superadmin;

        return new ReportFilterViewModel
        {
            Report = report,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Source = source,
            Status = status,
            // An Account has no other account to look at, so the picker is Superadmin-only and
            // the value is dropped rather than trusted.
            AccountId = isSuperadmin ? accountId : null,
            IsSuperadmin = isSuperadmin,
            Accounts = isSuperadmin
                ? (await _accounts.GetOptionsAsync(ct)).Select(a => new AccountOption(a.Id, a.Username)).ToList()
                : Array.Empty<AccountOption>(),
        };
    }
}
