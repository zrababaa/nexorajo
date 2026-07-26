using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Balance ledger entry. Every balance mutation in the app must go through
/// IBalanceLedgerService, which writes the balance change and this row atomically.
/// </summary>
public class Transaction : AuditableEntity, IHasCreator
{
    public int UserId { get; set; }
    public int CreatedByUserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionKind Kind { get; set; }
    public TransactionSource Source { get; set; }
    public string? RelatedBatchId { get; set; }
}
