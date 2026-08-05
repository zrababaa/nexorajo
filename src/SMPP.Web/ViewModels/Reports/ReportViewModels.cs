using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Reports;
using SMPP.Domain.Enums;

namespace SMPP.Web.ViewModels.Reports;

/// <summary>
/// The filter bar, shared by every tab so switching reports keeps the range you were looking
/// at. <see cref="Accounts"/> is populated for Superadmin only - an Account has nothing to
/// choose between.
/// </summary>
public class ReportFilterViewModel
{
    public ReportType Report { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public MessageSource? Source { get; set; }
    public MessageStatus? Status { get; set; }
    public int? AccountId { get; set; }

    public bool IsSuperadmin { get; set; }
    public IReadOnlyList<AccountOption> Accounts { get; set; } = Array.Empty<AccountOption>();

    /// <summary>Which filter inputs the current report actually honors - the rest are hidden.</summary>
    public bool ShowChannel => Report is ReportType.Messages or ReportType.DailyTraffic or ReportType.Batches or ReportType.AccountUsage;

    public bool ShowStatus => Report is ReportType.Messages or ReportType.DailyTraffic;

    public ReportFilterDto ToFilter() => new(DateFrom, DateTo, Source, Status, AccountId);

    /// <summary>Route values that carry the current filter onto the tab links and export buttons.</summary>
    public Dictionary<string, string> RouteValues(ReportType? report = null)
    {
        var route = new Dictionary<string, string> { ["report"] = (report ?? Report).ToString() };

        if (DateFrom.HasValue) route["dateFrom"] = DateFrom.Value.ToString("yyyy-MM-dd");
        if (DateTo.HasValue) route["dateTo"] = DateTo.Value.ToString("yyyy-MM-dd");
        if (Source.HasValue) route["source"] = Source.Value.ToString();
        if (Status.HasValue) route["status"] = Status.Value.ToString();
        if (AccountId.HasValue) route["accountId"] = AccountId.Value.ToString();

        return route;
    }
}

public record AccountOption(int Id, string Username);

public class ReportsViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();

    public PagedResult<HistoryExportRowDto> Messages { get; set; } = new();
    public PagedResult<DailyTrafficRowDto> DailyTraffic { get; set; } = new();
    public DailyTrafficTotals DailyTrafficTotals { get; set; } = new(0, 0, 0, 0, 0, 0, 0);
    public PagedResult<BatchReportRowDto> Batches { get; set; } = new();
    public BatchTotals BatchTotals { get; set; } = new(0, 0, 0, 0, 0m);
    public PagedResult<AccountUsageRowDto> AccountUsage { get; set; } = new();
    public AccountUsageTotals AccountUsageTotals { get; set; } = new(0, 0, 0, 0m, 0m, 0m);
    public PagedResult<TransactionReportRowDto> Transactions { get; set; } = new();
    public TransactionTotals TransactionTotals { get; set; } = new(0m);
    public PagedResult<CreditRequestRowDto> CreditRequests { get; set; } = new();
    public CreditRequestTotals CreditRequestTotals { get; set; } = new(0m);
}
