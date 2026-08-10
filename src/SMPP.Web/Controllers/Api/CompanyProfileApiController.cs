using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.CompanyProfiles;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record UpdateCompanyProfileApiRequest
{
    [MaxLength(100)]
    public string? RegistrationId { get; init; }

    [MaxLength(200)]
    public string? CompanyName { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(50)]
    public string? Phone { get; init; }

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(200)]
    public string? Website { get; init; }

    public string? Description { get; init; }

    /// <summary>Path returned by the logo-upload endpoint. Optional.</summary>
    public string? LogoPath { get; init; }
}

/// <summary>Multipart form carrying a logo or a supporting document.</summary>
public class UploadCompanyFileApiRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

public record AddCompanyDocumentApiRequest
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    public string FilePath { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }
}

/// <summary>
/// An account's company profile: registration details, a logo, and supporting documents.
/// Superadmin-only, managed on a specific account's behalf from the Accounts page - there is no
/// Account self-service access.
/// </summary>
[Route("api/v1/accounts/{accountId:int}/company-profile")]
[Tags("Company Profile")]
[Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
public class CompanyProfileApiController : ApiControllerBase
{
    private static readonly string[] LogoExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
    private const long MaxLogoBytes = 5 * 1024 * 1024;
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    private readonly ICompanyProfileService _profiles;
    private readonly IAccountService _accounts;
    private readonly IFileStorageService _fileStorage;

    public CompanyProfileApiController(ICompanyProfileService profiles, IAccountService accounts, IFileStorageService fileStorage)
    {
        _profiles = profiles;
        _accounts = accounts;
        _fileStorage = fileStorage;
    }

    /// <summary>The account's company profile, created with company mode inactive on first access.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int accountId, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        return notFound ?? Ok(await _profiles.GetAsync(accountId, ct));
    }

    /// <summary>Saves the account's company details. Company mode does not need to be active to edit them.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int accountId, [FromBody] UpdateCompanyProfileApiRequest request, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        return notFound ?? Ok(await _profiles.UpdateAsync(accountId, new UpdateCompanyProfileRequest(
            request.RegistrationId, request.CompanyName, request.Address, request.Phone,
            request.Email, request.Website, request.Description, request.LogoPath), ct));
    }

    /// <summary>Turns company mode on.</summary>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(int accountId, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        return notFound ?? Ok(await _profiles.ActivateAsync(accountId, ct));
    }

    /// <summary>Turns company mode off. Profile data and documents are kept, not deleted.</summary>
    [HttpPost("deactivate")]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(int accountId, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        return notFound ?? Ok(await _profiles.DeactivateAsync(accountId, ct));
    }

    /// <summary>
    /// Uploads a logo image and returns the stored path to pass as <c>logoPath</c> when saving
    /// the profile. Kept separate so saving the profile stays a plain JSON call.
    /// </summary>
    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadedFileApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadLogo([FromForm] UploadCompanyFileApiRequest request, CancellationToken ct)
    {
        var validation = ValidateFile(request.File, LogoExtensions, MaxLogoBytes);
        if (validation is not null)
        {
            return BadRequest(new ApiErrorResponse(validation));
        }

        await using var stream = request.File.OpenReadStream();
        var path = await _fileStorage.SaveAsync(stream, request.File.FileName, "crm/logos", ct);
        return Ok(new UploadedFileApiResponse(path));
    }

    /// <summary>The account's supporting documents.</summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(int accountId, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        return notFound ?? Ok(await _profiles.GetDocumentsAsync(accountId, ct));
    }

    /// <summary>
    /// Uploads a document file and returns the stored path to pass to <c>POST documents</c>.
    /// </summary>
    [HttpPost("documents/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadedFileApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadCompanyFileApiRequest request, CancellationToken ct)
    {
        var validation = ValidateFile(request.File, DocumentExtensions, MaxDocumentBytes);
        if (validation is not null)
        {
            return BadRequest(new ApiErrorResponse(validation));
        }

        await using var stream = request.File.OpenReadStream();
        var path = await _fileStorage.SaveAsync(stream, request.File.FileName, "crm/documents", ct);
        return Ok(new UploadedFileApiResponse(path));
    }

    /// <summary>Attaches an uploaded document file to the account's company profile.</summary>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(CompanyDocumentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDocument(int accountId, [FromBody] AddCompanyDocumentApiRequest request, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        if (notFound is not null)
        {
            return notFound;
        }

        var document = await _profiles.AddDocumentAsync(accountId, new AddCompanyDocumentRequest(
            request.FileName, request.FilePath, request.FileSizeBytes), ct);
        return Created($"/api/v1/accounts/{accountId}/company-profile/documents/{document.Id}", document);
    }

    /// <summary>Removes a document from the account's company profile.</summary>
    [HttpDelete("documents/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(int accountId, int id, CancellationToken ct)
    {
        var notFound = await EnsureAccountExistsAsync(accountId, ct);
        if (notFound is not null)
        {
            return notFound;
        }

        await _profiles.DeleteDocumentAsync(accountId, id, ct);
        return NoContent();
    }

    private async Task<IActionResult?> EnsureAccountExistsAsync(int accountId, CancellationToken ct) =>
        await _accounts.GetByIdAsync(accountId, ct) is null
            ? NotFound(new ApiErrorResponse("Account not found."))
            : null;

    private static string? ValidateFile(IFormFile file, string[] allowedExtensions, long maxBytes)
    {
        if (file.Length == 0)
        {
            return "The uploaded file is empty.";
        }

        if (file.Length > maxBytes)
        {
            return $"The uploaded file exceeds the {maxBytes / (1024 * 1024)} MB limit.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}.";
        }

        return null;
    }
}
