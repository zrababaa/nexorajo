using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Payments;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly SmppDbContext _db;
    private readonly IBalanceLedgerService _ledger;

    public PaymentService(SmppDbContext db, IBalanceLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<int> SubmitRequestAsync(int accountUserId, SubmitPaymentRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppException("Requested amount must be greater than zero.");
        }

        var payment = new Payment
        {
            Amount = request.Amount,
            Method = request.Method,
            TransactionRef = request.TransactionRef,
            Note = request.Note,
            ProofFilePath = request.ProofFilePath,
            Status = PaymentStatus.Pending,
            SubmittedByUserId = accountUserId,
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        return payment.Id;
    }

    public async Task<PagedResult<PaymentListItemDto>> GetMyRequestsAsync(int accountUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Payments
            .Where(p => p.SubmittedByUserId == accountUserId)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<PaymentListItemDto>> GetForReviewAsync(PaymentStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Payment> query = _db.Payments;
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }
        query = query.OrderBy(p => p.Status == PaymentStatus.Pending ? 0 : 1).ThenByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, page, pageSize, ct);
    }

    public async Task ApproveAsync(int paymentId, int reviewerUserId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new AppException("Credit request not found.");

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new AppException("This request has already been reviewed.");
        }

        await _ledger.ApplyAsync(new BalanceLedgerRequest(
            payment.SubmittedByUserId, reviewerUserId, payment.Amount, TransactionKind.Credit, TransactionSource.PaymentApproval), ct);

        payment.Status = PaymentStatus.Approved;
        payment.ReviewedByUserId = reviewerUserId;
        payment.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(int paymentId, int reviewerUserId, string? reviewNote, CancellationToken ct = default)
    {
        var payment = await _db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new AppException("Credit request not found.");

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new AppException("This request has already been reviewed.");
        }

        payment.Status = PaymentStatus.Rejected;
        payment.ReviewedByUserId = reviewerUserId;
        payment.ReviewedAt = DateTime.UtcNow;
        payment.ReviewNote = reviewNote;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<PagedResult<PaymentListItemDto>> ToPagedAsync(IQueryable<Payment> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Users, p => p.SubmittedByUserId, u => u.Id, (p, u) => new PaymentListItemDto(
                p.Id, p.Amount, p.Method, p.TransactionRef, p.ProofFilePath, p.Note, p.Status, p.ReviewNote,
                p.CreatedAt, p.ReviewedAt, p.SubmittedByUserId, u.UserName!))
            .ToListAsync(ct);

        return new PagedResult<PaymentListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }
}
