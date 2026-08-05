using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Payments;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Extensions;
using SMPP.Web.ViewModels.Payments;

namespace SMPP.Web.Controllers;

/// <summary>
/// Self-service credit top-up: an Account submits a manual proof-of-payment request here and
/// tracks its status; a Superadmin reviews the platform-wide queue and approves (crediting the
/// balance) or rejects it. See IPaymentService for the approval/ledger contract.
/// </summary>
public class PaymentsController : Controller
{
    private const int PageSize = 15;

    private readonly IPaymentService _paymentService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(IPaymentService paymentService, IFileStorageService fileStorage, ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Superadmin)
        {
            return RedirectToAction(nameof(Review));
        }

        var result = await _paymentService.GetMyRequestsAsync(_currentUser.UserId, page, PageSize, ct);
        ViewBag.Requests = result;
        return View(new SubmitPaymentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubmitPaymentViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var result = await _paymentService.GetMyRequestsAsync(_currentUser.UserId, 1, PageSize, ct);
            ViewBag.Requests = result;
            return View(nameof(Index), model);
        }

        try
        {
            string? proofPath = null;
            if (model.ProofFile is { Length: > 0 })
            {
                proofPath = await _fileStorage.SaveAsync(model.ProofFile.OpenReadStream(), model.ProofFile.FileName, "payments", ct);
            }

            await _paymentService.SubmitRequestAsync(_currentUser.UserId, new SubmitPaymentRequest(
                model.Amount, model.Method, model.TransactionRef, model.Note, proofPath), ct);

            TempData["Success"] = "Credit request submitted. A Superadmin will review it shortly.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Superadmin)]
    public async Task<IActionResult> Review(PaymentStatus? status, int page = 1, CancellationToken ct = default)
    {
        var result = await _paymentService.GetForReviewAsync(status, page, PageSize, ct);
        ViewBag.StatusFilter = status;

        // Live-filtering htmx requests only need the results fragment re-rendered, not the whole page.
        return Request.IsHtmxFragment("payments-review-results") ? PartialView("_ReviewResults", result) : View(result);
    }

    [Authorize(Roles = RoleNames.Superadmin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        try
        {
            await _paymentService.ApproveAsync(id, _currentUser.UserId, ct);
            TempData["Success"] = "Credit request approved and balance credited.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Review));
    }

    [Authorize(Roles = RoleNames.Superadmin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectPaymentViewModel model, CancellationToken ct)
    {
        try
        {
            await _paymentService.RejectAsync(model.PaymentId, _currentUser.UserId, model.ReviewNote, ct);
            TempData["Success"] = "Credit request rejected.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Review));
    }
}
