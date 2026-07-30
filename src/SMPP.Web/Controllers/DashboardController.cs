using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Dashboard;
using SMPP.Web.ViewModels.Dashboard;

namespace SMPP.Web.Controllers;

public class DashboardController : Controller
{
    private const int TrendDays = 14;

    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var summary = await _dashboardService.GetSummaryAsync(_currentUser.UserId, _currentUser.Role, ct);
        var trend = await _dashboardService.GetSendsTrendAsync(_currentUser.UserId, _currentUser.Role, TrendDays, ct);
        var statusBreakdown = await _dashboardService.GetStatusBreakdownAsync(_currentUser.UserId, _currentUser.Role, ct);
        var sourceBreakdown = await _dashboardService.GetSourceBreakdownAsync(_currentUser.UserId, _currentUser.Role, ct);

        return View(new DashboardViewModel
        {
            Summary = summary,
            Trend = trend,
            StatusBreakdown = statusBreakdown,
            SourceBreakdown = sourceBreakdown,
        });
    }
}
