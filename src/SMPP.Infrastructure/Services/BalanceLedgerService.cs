using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// The single entry point for every balance mutation in the app - see IBalanceLedgerService.
/// No other code should write to ApplicationUser.Balance directly.
/// </summary>
public class BalanceLedgerService : IBalanceLedgerService
{
    private readonly SmppDbContext _db;

    public BalanceLedgerService(SmppDbContext db)
    {
        _db = db;
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
