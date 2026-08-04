using SMPP.Application.Reports;
using SMPP.Web.ViewModels.Reports;

namespace SMPP.Web.ViewModels.AccountsLogs;

/// <summary>Every bulk-send batch across every account - a Superadmin-only slice of the Batches report.</summary>
public class AccountsLogsViewModel
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? AccountId { get; set; }

    public IReadOnlyList<AccountOption> Accounts { get; set; } = Array.Empty<AccountOption>();
    public IReadOnlyList<BatchReportRowDto> Batches { get; set; } = Array.Empty<BatchReportRowDto>();

    public Dictionary<string, string> RouteValues()
    {
        var route = new Dictionary<string, string>();
        if (DateFrom.HasValue) route["dateFrom"] = DateFrom.Value.ToString("yyyy-MM-dd");
        if (DateTo.HasValue) route["dateTo"] = DateTo.Value.ToString("yyyy-MM-dd");
        if (AccountId.HasValue) route["accountId"] = AccountId.Value.ToString();
        return route;
    }
}
