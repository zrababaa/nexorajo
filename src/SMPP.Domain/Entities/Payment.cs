using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// Manual proof-of-payment submission (no real payment gateway exists upstream).
/// Approval must atomically credit the submitter's balance via IBalanceLedgerService
/// (legacy approval only updated status/date window and never touched balance).
/// </summary>
public class Payment : AuditableEntity
{
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionRef { get; set; }
    public string? ProofFilePath { get; set; }
    public string? Note { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public int SubmittedByUserId { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
