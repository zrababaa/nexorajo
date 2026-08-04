using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Dashboard;

namespace SMPP.Web.Controllers.Api;

public record DashboardApiResponse(
    DashboardSummaryDto Summary,
    IReadOnlyList<DashboardTrendPointDto> Trend,
    IReadOnlyList<DashboardStatusSliceDto> StatusBreakdown,
    IReadOnlyList<DashboardSourceSliceDto> SourceBreakdown);

/// <summary>
/// The KPIs and chart series behind the dashboard, scoped by role: an account sees its own
/// activity, a Superadmin sees the platform's.
/// </summary>
[Route("api/v1/dashboard")]
[Tags("Dashboard")]
public class DashboardApiController : ApiControllerBase
{
    private const int DefaultTrendDays = 14;

    private readonly IDashboardService _dashboard;

    public DashboardApiController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Everything the dashboard screen shows, in one call.</summary>
    /// <param name="trendDays">How many days of send history to include in the trend series (1-90).</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int trendDays = DefaultTrendDays, CancellationToken ct = default) =>
        Ok(new DashboardApiResponse(
            await _dashboard.GetSummaryAsync(CurrentUserId, CurrentRole, ct),
            await _dashboard.GetSendsTrendAsync(CurrentUserId, CurrentRole, ClampDays(trendDays), ct),
            await _dashboard.GetStatusBreakdownAsync(CurrentUserId, CurrentRole, ct),
            await _dashboard.GetSourceBreakdownAsync(CurrentUserId, CurrentRole, ct)));

    /// <summary>Balance, sends today, delivery rate, and pending payment count.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        Ok(await _dashboard.GetSummaryAsync(CurrentUserId, CurrentRole, ct));

    /// <summary>Daily send volume, oldest first.</summary>
    /// <param name="days">How many days back to go (1-90).</param>
    /// <param name="ct"></param>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardTrendPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Trend([FromQuery] int days = DefaultTrendDays, CancellationToken ct = default) =>
        Ok(await _dashboard.GetSendsTrendAsync(CurrentUserId, CurrentRole, ClampDays(days), ct));

    /// <summary>Message counts per delivery status.</summary>
    [HttpGet("status-breakdown")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardStatusSliceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StatusBreakdown(CancellationToken ct) =>
        Ok(await _dashboard.GetStatusBreakdownAsync(CurrentUserId, CurrentRole, ct));

    /// <summary>Message counts per send channel.</summary>
    [HttpGet("source-breakdown")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardSourceSliceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SourceBreakdown(CancellationToken ct) =>
        Ok(await _dashboard.GetSourceBreakdownAsync(CurrentUserId, CurrentRole, ct));

    private static int ClampDays(int days) => Math.Clamp(days, 1, 90);
}
