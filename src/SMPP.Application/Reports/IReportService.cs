using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Enums;

namespace SMPP.Application.Reports;

/// <summary>
/// Every report on the Reports tab, scoped the same way the rest of the app is: an Account only
/// ever sees its own rows, a Superadmin sees all of them (IUserScopeResolver).
///
/// Each report has a typed getter for the on-screen table - paged, so a filter that matches
/// thousands of rows still only pulls one page's worth per request - and is also reachable
/// through <see cref="BuildExportAsync"/>, which flattens the same rows - same filters, same
/// scope - so an export always matches what the screen showed (export is intentionally
/// unbounded-but-capped: a spreadsheet is meant to be dumped, a browser tab is not).
/// </summary>
public interface IReportService
{
    Task<(PagedResult<DailyTrafficRowDto> Page, DailyTrafficTotals Totals)> GetDailyTrafficAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    Task<(PagedResult<BatchReportRowDto> Page, BatchTotals Totals)> GetBatchesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Superadmin-grain report; an Account calling it gets the single row for itself.</summary>
    Task<(PagedResult<AccountUsageRowDto> Page, AccountUsageTotals Totals)> GetAccountUsageAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    Task<(PagedResult<TransactionReportRowDto> Page, TransactionTotals Totals)> GetTransactionsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    Task<(PagedResult<CreditRequestRowDto> Page, CreditRequestTotals Totals)> GetCreditRequestsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Per-recipient message rows, reusing the History query so the two never diverge.</summary>
    Task<PagedResult<HistoryExportRowDto>> GetMessagesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The chosen report flattened to headers + string rows, ready for CSV or Excel.</summary>
    Task<ReportTable> BuildExportAsync(
        ReportType type, int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);
}
