using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// One row per change to the AdminBudget pool - a manual edit by a Superadmin, or an automatic
/// deduction made when an account is granted credit. See IAdminBudgetService.
/// </summary>
public class AdminBudgetLog : AuditableEntity
{
    public int PerformedByUserId { get; set; }
    public TransactionKind Kind { get; set; }
    public TransactionSource Source { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    /// <summary>The account that was credited, set only on an automatic deduction row.</summary>
    public int? RelatedUserId { get; set; }
    public string? Note { get; set; }
}
