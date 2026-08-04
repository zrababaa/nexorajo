using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SMPP.Application.Abstractions;
using SMPP.Application.AdminBudget;
using SMPP.Application.Common;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// The single entry point for every balance mutation in the app - see IBalanceLedgerService.
/// No other code should write to ApplicationUser.Balance directly. A credit also draws the same
/// amount from the shared AdminBudget pool (see IAdminBudgetService) - a grant that would take
/// the pool below zero is rejected the same way an insufficient user balance is.
/// </summary>
public class BalanceLedgerService : IBalanceLedgerService
{
    private readonly SmppDbContext _db;
    private readonly IAdminBudgetService _adminBudget;

    public BalanceLedgerService(SmppDbContext db, IAdminBudgetService adminBudget)
    {
        _db = db;
        _adminBudget = adminBudget;
    }

    public async Task<decimal> ApplyAsync(BalanceLedgerRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppException("Transaction amount must be greater than zero.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);

            var user = await _db.Users.SingleAsync(u => u.Id == request.UserId, ct);

            var signedAmount = request.Kind == TransactionKind.Credit ? request.Amount : -request.Amount;
            var newBalance = user.Balance + signedAmount;

            if (newBalance < 0)
            {
                throw new AppException(
                    $"Debit of {request.Amount:0.####} would take user {user.Id}'s balance below zero (current: {user.Balance:0.####}).");
            }

            user.Balance = newBalance;

            if (request.Kind == TransactionKind.Credit)
            {
                await _adminBudget.ReserveAsync(request.PerformedByUserId, request.UserId, request.Amount, request.Source, ct);
            }

            _db.Transactions.Add(new Transaction
            {
                UserId = request.UserId,
                CreatedByUserId = request.PerformedByUserId,
                Amount = request.Amount,
                Kind = request.Kind,
                Source = request.Source,
                RelatedBatchId = request.RelatedBatchId,
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return newBalance;
        });
    }
}
