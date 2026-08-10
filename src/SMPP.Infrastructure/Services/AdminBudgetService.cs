using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SMPP.Application.AdminBudget;
using SMPP.Application.Common;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class AdminBudgetService : IAdminBudgetService
{
    private const int SingletonId = 1;

    private readonly SmppDbContext _db;

    public AdminBudgetService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBalanceAsync(CancellationToken ct = default) =>
        (await _db.AdminBudgets.AsNoTracking().SingleAsync(b => b.Id == SingletonId, ct)).Balance;

    public async Task<PagedResult<AdminBudgetLogRowDto>> GetLogAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.AdminBudgetLogs.AsNoTracking().OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = rows.Select(r => r.PerformedByUserId)
            .Concat(rows.Where(r => r.RelatedUserId.HasValue).Select(r => r.RelatedUserId!.Value))
            .Distinct()
            .ToList();

        var usernames = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName!, ct);

        var items = rows
            .Select(r => new AdminBudgetLogRowDto(
                r.Id,
                r.CreatedAt,
                usernames.GetValueOrDefault(r.PerformedByUserId, r.PerformedByUserId.ToString()),
                r.Kind,
                r.Source,
                r.Amount,
                r.BalanceAfter,
                r.RelatedUserId.HasValue ? usernames.GetValueOrDefault(r.RelatedUserId.Value, r.RelatedUserId.Value.ToString()) : null,
                r.Note))
            .ToList();

        return new PagedResult<AdminBudgetLogRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<decimal> SetBalanceAsync(int performedByUserId, decimal newBalance, string? note, CancellationToken ct = default)
    {
        if (newBalance < 0)
        {
            throw new AppException("The admin budget cannot be set to a negative number.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);

            var budget = await _db.AdminBudgets.SingleAsync(b => b.Id == SingletonId, ct);
            var delta = newBalance - budget.Balance;
            budget.Balance = newBalance;

            if (delta != 0)
            {
                _db.AdminBudgetLogs.Add(new AdminBudgetLog
                {
                    PerformedByUserId = performedByUserId,
                    Kind = delta > 0 ? TransactionKind.Credit : TransactionKind.Debit,
                    Source = TransactionSource.ManualAdjustment,
                    Amount = Math.Abs(delta),
                    BalanceAfter = newBalance,
                    RelatedUserId = null,
                    Note = note,
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return newBalance;
        });
    }

    public async Task ReserveAsync(int performedByUserId, int creditedUserId, decimal amount, TransactionSource source, CancellationToken ct = default)
    {
        var budget = await _db.AdminBudgets.SingleAsync(b => b.Id == SingletonId, ct);
        var newBalance = budget.Balance - amount;

        if (newBalance < 0)
        {
            throw new AppException(
                $"Insufficient admin budget: crediting this account costs {amount:0.####} and the remaining pool is {budget.Balance:0.####}.");
        }

        budget.Balance = newBalance;

        _db.AdminBudgetLogs.Add(new AdminBudgetLog
        {
            PerformedByUserId = performedByUserId,
            Kind = TransactionKind.Debit,
            Source = source,
            Amount = amount,
            BalanceAfter = newBalance,
            RelatedUserId = creditedUserId,
        });
    }

    public async Task ReleaseAsync(int performedByUserId, int debitedUserId, decimal amount, TransactionSource source, CancellationToken ct = default)
    {
        var budget = await _db.AdminBudgets.SingleAsync(b => b.Id == SingletonId, ct);
        var newBalance = budget.Balance + amount;

        budget.Balance = newBalance;

        _db.AdminBudgetLogs.Add(new AdminBudgetLog
        {
            PerformedByUserId = performedByUserId,
            Kind = TransactionKind.Credit,
            Source = source,
            Amount = amount,
            BalanceAfter = newBalance,
            RelatedUserId = debitedUserId,
        });
    }
}
