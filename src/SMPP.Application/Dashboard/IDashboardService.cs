using SMPP.Domain.Enums;

namespace SMPP.Application.Dashboard;

/// <summary>
/// Real role-scoped KPIs, replacing legacy's generic AdminLTE placeholder dashboard (no data
/// at all). Scoping follows IUserScopeResolver: EndUser sees only their own activity,
/// WhiteLabelAdmin sees themselves + their EndUsers, Superadmin sees everyone.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int currentUserId, UserRole role, CancellationToken ct = default);
}
