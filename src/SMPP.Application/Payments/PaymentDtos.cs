using SMPP.Domain.Enums;

namespace SMPP.Application.Payments;

public record PaymentListItemDto(
    int Id,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionRef,
    string? ProofFilePath,
    string? Note,
    PaymentStatus Status,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    int SubmittedByUserId,
    string SubmittedByUsername);

public record SubmitPaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    string? TransactionRef,
    string? Note,
    string? ProofFilePath);
