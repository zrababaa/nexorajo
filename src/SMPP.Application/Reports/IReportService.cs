using SMPP.Application.History;
using SMPP.Domain.Enums;

namespace SMPP.Application.Reports;

/// <summary>
/// Every report on the Reports tab, scoped the same way the rest of the app is: an Account only
/// ever sees its own rows, a Superadmin sees all of them (IUserScopeResolver).
///
/// Each report has a typed getter for the on-screen table and is also reachable through
/// <see cref="BuildExportAsync"/>, which flattens the same rows - same filters, same scope - so
/// an export always matches what the screen showed.
/// </summary>
public interface IReportService
{
    Task<IReadOnlyList<DailyTrafficRowDto>> GetDailyTrafficAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    Task<IReadOnlyList<BatchReportRowDto>> GetBatchesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    /// <summary>Superadmin-grain report; an Account calling it gets the single row for itself.</summary>
    Task<IReadOnlyList<AccountUsageRowDto>> GetAccountUsageAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    Task<IReadOnlyList<TransactionReportRowDto>> GetTransactionsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    Task<IReadOnlyList<CreditRequestRowDto>> GetCreditRequestsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    /// <summary>Per-recipient message rows, reusing the History query so the two never diverge.</summary>
    Task<IReadOnlyList<HistoryExportRowDto>> GetMessagesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);

    /// <summary>The chosen report flattened to headers + string rows, ready for CSV or Excel.</summary>
    Task<ReportTable> BuildExportAsync(
        ReportType type, int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default);
}
