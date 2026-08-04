using SMPP.Domain.Enums;

namespace SMPP.Application.AdminBudget;

public record AdminBudgetLogRowDto(
    int Id,
    DateTime CreatedAt,
    string PerformedByUsername,
    TransactionKind Kind,
    TransactionSource Source,
    decimal Amount,
    decimal BalanceAfter,
    string? RelatedUsername,
    string? Note);
