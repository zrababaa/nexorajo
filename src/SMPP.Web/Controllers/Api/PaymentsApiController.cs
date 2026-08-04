using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Payments;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record SubmitPaymentApiRequest
{
    [Range(0.0001, double.MaxValue)]
    public decimal Amount { get; init; }

    public PaymentMethod Method { get; init; }

    /// <summary>The reference the payment provider gave you.</summary>
    public string? TransactionRef { get; init; }

    public string? Note { get; init; }

    /// <summary>Path returned by the proof-upload endpoint. Optional.</summary>
    public string? ProofFilePath { get; init; }
}

/// <summary>Multipart form carrying a proof-of-payment file.</summary>
public class UploadProofApiRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

public record UploadedFileApiResponse(string Path);

public record RejectPaymentApiRequest
{
    public string? ReviewNote { get; init; }
}

public record PaymentSubmittedApiResponse(int Id);

/// <summary>
/// Credit top-ups. An account uploads its proof of payment, submits a request, and tracks it;
/// a Superadmin works the queue and approves - which credits the balance through the ledger -
/// or rejects with a note.
/// </summary>
[Route("api/v1/payments")]
[Tags("Payments")]
public class PaymentsApiController : ApiControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IFileStorageService _fileStorage;

    public PaymentsApiController(IPaymentService payments, IFileStorageService fileStorage)
    {
        _payments = payments;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Uploads a proof-of-payment file and returns the stored path to pass as
    /// <c>proofFilePath</c> when submitting the request. Kept separate so submitting a request
    /// stays a plain JSON call.
    /// </summary>
    [HttpPost("proof")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadedFileApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadProof([FromForm] UploadProofApiRequest request, CancellationToken ct)
    {
        if (request.File.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("The uploaded file is empty."));
        }

        await using var stream = request.File.OpenReadStream();
        var path = await _fileStorage.SaveAsync(stream, request.File.FileName, "payments", ct);
        return Ok(new UploadedFileApiResponse(path));
    }

    /// <summary>Submits a credit request for review.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentSubmittedApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit([FromBody] SubmitPaymentApiRequest request, CancellationToken ct)
    {
        var id = await _payments.SubmitRequestAsync(CurrentUserId, new SubmitPaymentRequest(
            request.Amount, request.Method, request.TransactionRef, request.Note, request.ProofFilePath), ct);

        return Created($"/api/v1/payments/{id}", new PaymentSubmittedApiResponse(id));
    }

    /// <summary>Your own credit requests and where each one stands.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResult<PaymentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _payments.GetMyRequestsAsync(CurrentUserId, Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>The platform-wide review queue, optionally narrowed to one status.</summary>
    [HttpGet("review")]
    [Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
    [ProducesResponseType(typeof(PagedResult<PaymentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForReview(
        [FromQuery] PaymentStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _payments.GetForReviewAsync(status, Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>Approves a request and credits the account's balance.</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await _payments.ApproveAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    /// <summary>Rejects a request, optionally with a note the account can see.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectPaymentApiRequest request, CancellationToken ct)
    {
        await _payments.RejectAsync(id, CurrentUserId, request.ReviewNote, ct);
        return NoContent();
    }
}
