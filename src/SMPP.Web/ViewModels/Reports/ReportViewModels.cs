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

    public IReadOnlyList<HistoryExportRowDto> Messages { get; set; } = Array.Empty<HistoryExportRowDto>();
    public IReadOnlyList<DailyTrafficRowDto> DailyTraffic { get; set; } = Array.Empty<DailyTrafficRowDto>();
    public IReadOnlyList<BatchReportRowDto> Batches { get; set; } = Array.Empty<BatchReportRowDto>();
    public IReadOnlyList<AccountUsageRowDto> AccountUsage { get; set; } = Array.Empty<AccountUsageRowDto>();
    public IReadOnlyList<TransactionReportRowDto> Transactions { get; set; } = Array.Empty<TransactionReportRowDto>();
    public IReadOnlyList<CreditRequestRowDto> CreditRequests { get; set; } = Array.Empty<CreditRequestRowDto>();
}
