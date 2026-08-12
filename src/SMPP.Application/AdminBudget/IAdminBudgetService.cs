using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.AdminBudget;

/// <summary>
/// The shared pool of credits a Superadmin can hand out to accounts. Every account credit -
/// manual, payment approval, or an account's initial balance at creation - must draw from this
/// pool through <see cref="ReserveAsync"/>; nothing else may touch the AdminBudget table.
/// </summary>
public interface IAdminBudgetService
{
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);

    Task<PagedResult<AdminBudgetLogRowDto>> GetLogAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Manual increase or decrease of the pool's total, made directly by a Superadmin. Never
    /// replaces the balance outright - <paramref name="amount"/> is always a positive magnitude,
    /// applied in the direction given by <paramref name="kind"/>.
    /// </summary>
    /// <exception cref="AppException">
    /// Thrown when <paramref name="amount"/> is not positive, or when a <see cref="TransactionKind.Debit"/>
    /// would take the balance below zero.
    /// </exception>
    Task<decimal> AdjustBalanceAsync(int performedByUserId, decimal amount, TransactionKind kind, string? note, CancellationToken ct = default);

    /// <summary>
    /// Draws <paramref name="amount"/> from the pool for a credit about to be granted to
    /// <paramref name="creditedUserId"/>. Mutates the tracked <c>AdminBudget</c>/<c>AdminBudgetLog</c>
    /// entities but does not save or open a transaction - the caller commits, so this composes
    /// inside an already-open transaction (see BalanceLedgerService).
    /// </summary>
    /// <exception cref="AppException">Thrown when the pool does not cover <paramref name="amount"/>.</exception>
    Task ReserveAsync(int performedByUserId, int creditedUserId, decimal amount, TransactionSource source, CancellationToken ct = default);

    /// <summary>
    /// Returns <paramref name="amount"/> to the pool after a Superadmin manually debits
    /// <paramref name="debitedUserId"/>'s balance - the mirror image of <see cref="ReserveAsync"/>.
    /// Mutates the tracked <c>AdminBudget</c>/<c>AdminBudgetLog</c> entities but does not save or
    /// open a transaction - the caller commits (see BalanceLedgerService).
    /// </summary>
    Task ReleaseAsync(int performedByUserId, int debitedUserId, decimal amount, TransactionSource source, CancellationToken ct = default);
}
