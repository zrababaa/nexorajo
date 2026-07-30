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

        int? totalAccounts = null;
        decimal? totalCreditsIssued = null;
        if (role == UserRole.Superadmin)
        {
            totalAccounts = await _db.Users.CountAsync(u => u.Role == UserRole.Account, ct);
            totalCreditsIssued = await _db.Users.Where(u => u.Role == UserRole.Account).SumAsync(u => (decimal?)u.Balance, ct) ?? 0m;
        }

        return new DashboardSummaryDto(balance, sendsToday, deliveryRate, pendingPayments, totalAccounts, totalCreditsIssued);
    }

    public async Task<IReadOnlyList<DashboardTrendPointDto>> GetSendsTrendAsync(int currentUserId, UserRole role, int days, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var sinceUtc = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var rows = await _db.Histories
            .Where(h => visibleUserIds.Contains(h.CreatedByUserId) && h.CreatedAt >= sinceUtc)
            .GroupBy(h => h.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                Delivered = g.Count(h => h.Status == MessageStatus.Delivered),
            })
            .ToListAsync(ct);

        var byDate = rows.ToDictionary(r => DateOnly.FromDateTime(r.Date));

        var points = new List<DashboardTrendPointDto>(days);
        for (var i = 0; i < days; i++)
        {
            var date = DateOnly.FromDateTime(sinceUtc.AddDays(i));
            points.Add(byDate.TryGetValue(date, out var r)
                ? new DashboardTrendPointDto(date, r.Total, r.Delivered)
                : new DashboardTrendPointDto(date, 0, 0));
        }

        return points;
    }

    public async Task<IReadOnlyList<DashboardStatusSliceDto>> GetStatusBreakdownAsync(int currentUserId, UserRole role, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var rows = await _db.Histories
            .Where(h => visibleUserIds.Contains(h.CreatedByUserId))
            .GroupBy(h => h.Status)
            .Select(g => new DashboardStatusSliceDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var byStatus = rows.ToDictionary(r => r.Status);
        return Enum.GetValues<MessageStatus>()
            .Select(s => byStatus.TryGetValue(s, out var slice) ? slice : new DashboardStatusSliceDto(s, 0))
            .ToList();
    }

    public async Task<IReadOnlyList<DashboardSourceSliceDto>> GetSourceBreakdownAsync(int currentUserId, UserRole role, CancellationToken ct = default)
    {
        var visibleUserIds = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        var rows = await _db.Histories
            .Where(h => visibleUserIds.Contains(h.CreatedByUserId))
            .GroupBy(h => h.Source)
            .Select(g => new DashboardSourceSliceDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var bySource = rows.ToDictionary(r => r.Source);
        return Enum.GetValues<MessageSource>()
            .Select(s => bySource.TryGetValue(s, out var slice) ? slice : new DashboardSourceSliceDto(s, 0))
            .ToList();
    }
}
