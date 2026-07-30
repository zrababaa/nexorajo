using SMPP.Application.Dashboard;

namespace SMPP.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public required DashboardSummaryDto Summary { get; init; }
    public required IReadOnlyList<DashboardTrendPointDto> Trend { get; init; }
    public required IReadOnlyList<DashboardStatusSliceDto> StatusBreakdown { get; init; }
    public required IReadOnlyList<DashboardSourceSliceDto> SourceBreakdown { get; init; }
}
