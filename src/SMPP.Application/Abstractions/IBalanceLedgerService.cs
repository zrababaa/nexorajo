using SMPP.Domain.Enums;

namespace SMPP.Application.Abstractions;

public record BalanceLedgerRequest(
    int UserId,
    int PerformedByUserId,
    decimal Amount,
    TransactionKind Kind,
    TransactionSource Source,
    string? RelatedBatchId = null);

/// <summary>
/// The single entry point for every balance mutation in the app. Atomically applies the
/// balance change and writes the matching Transaction ledger row in one DB transaction,
/// and enforces a no-negative-balance floor for every role. Fixes legacy's inconsistent
/// bookkeeping (debits logged without a ledger row on some paths, an asymmetric floor
/// check between admin and end-user debit actions).
/// </summary>
public interface IBalanceLedgerService
{
    /// <summary>Applies the mutation and returns the user's resulting balance.</summary>
    /// <exception cref="SMPP.Application.Common.AppException">
    /// Thrown when a debit would take the user's balance below zero.
    /// </exception>
    Task<decimal> ApplyAsync(BalanceLedgerRequest request, CancellationToken ct = default);
}
