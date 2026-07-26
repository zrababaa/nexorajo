using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Dashboard;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly SmppDbContext _db;
    private readonly IUserScopeResolver _scopeResolver;

    public DashboardService(SmppDbContext db, IUserScopeResolver scopeResolver)
    {
        _db = db;
        _scopeResolver = scopeResolver;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int currentUserId, UserRole role, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var balance = await _db.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.Balance)
            .FirstOrDefaultAsync(ct);

        var todayUtc = DateTime.UtcNow.Date;
        var sendsToday = await _db.Histories
            .CountAsync(h => visibleUserIds.Contains(h.CreatedByUserId) && h.CreatedAt >= todayUtc, ct);

        var delivered = await _db.Histories
            .CountAsync(h => visibleUserIds.Contains(h.CreatedByUserId) && h.Status == MessageStatus.Delivered, ct);
        var settled = await _db.Histories
            .CountAsync(h => visibleUserIds.Contains(h.CreatedByUserId) && h.Status != MessageStatus.Processing, ct);
        double? deliveryRate = settled == 0 ? null : Math.Round(delivered * 100.0 / settled, 1);

        var pendingPayments = await _db.Payments
            .CountAsync(p => visibleUserIds.Contains(p.SubmittedByUserId) && p.Status == PaymentStatus.Pending, ct);

        return new DashboardSummaryDto(balance, sendsToday, deliveryRate, pendingPayments);
    }
}
