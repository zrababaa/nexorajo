using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.Payments;

/// <summary>
/// Self-service credit top-up requests: an Account submits a manual proof-of-payment, a
/// Superadmin approves (which credits the balance via IBalanceLedgerService) or rejects it.
/// </summary>
public interface IPaymentService
{
    Task<int> SubmitRequestAsync(int accountUserId, SubmitPaymentRequest request, CancellationToken ct = default);

    Task<PagedResult<PaymentListItemDto>> GetMyRequestsAsync(int accountUserId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Superadmin-wide queue, optionally filtered by status (defaults to showing everything).</summary>
    Task<PagedResult<PaymentListItemDto>> GetForReviewAsync(PaymentStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task ApproveAsync(int paymentId, int reviewerUserId, CancellationToken ct = default);

    Task RejectAsync(int paymentId, int reviewerUserId, string? reviewNote, CancellationToken ct = default);
}
