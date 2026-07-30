using SMPP.Domain.Enums;

namespace SMPP.Application.Dashboard;

/// <summary>
/// Real role-scoped KPIs and chart data, replacing legacy's generic AdminLTE placeholder
/// dashboard (no data at all). Scoping follows IUserScopeResolver: Account sees only its own
/// activity, Superadmin sees everyone's.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int currentUserId, UserRole role, CancellationToken ct = default);

    /// <summary>Daily send volume for the last <paramref name="days"/> days (including today), oldest first.</summary>
    Task<IReadOnlyList<DashboardTrendPointDto>> GetSendsTrendAsync(int currentUserId, UserRole role, int days, CancellationToken ct = default);

    Task<IReadOnlyList<DashboardStatusSliceDto>> GetStatusBreakdownAsync(int currentUserId, UserRole role, CancellationToken ct = default);

    Task<IReadOnlyList<DashboardSourceSliceDto>> GetSourceBreakdownAsync(int currentUserId, UserRole role, CancellationToken ct = default);
}
