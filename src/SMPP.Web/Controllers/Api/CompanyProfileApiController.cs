using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
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
/// An Account's self-service company profile: registration details, a logo, and supporting
/// documents. No Superadmin approval step - the Account activates/deactivates and edits it
/// directly, and only ever sees its own profile.
/// </summary>
[Route("api/v1/company-profile")]
[Tags("Company Profile")]
[Authorize(Roles = RoleNames.Account, AuthenticationSchemes = ApiAuth.Schemes)]
public class CompanyProfileApiController : ApiControllerBase
{
    private static readonly string[] LogoExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
    private const long MaxLogoBytes = 5 * 1024 * 1024;
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    private readonly ICompanyProfileService _profiles;
    private readonly IFileStorageService _fileStorage;

    public CompanyProfileApiController(ICompanyProfileService profiles, IFileStorageService fileStorage)
    {
        _profiles = profiles;
        _fileStorage = fileStorage;
    }

    /// <summary>Your company profile, created with company mode inactive on first access.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _profiles.GetAsync(CurrentUserId, ct));

    /// <summary>Saves your company details. Company mode does not need to be active to edit them.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyProfileApiRequest request, CancellationToken ct) =>
        Ok(await _profiles.UpdateAsync(CurrentUserId, new UpdateCompanyProfileRequest(
            request.RegistrationId, request.CompanyName, request.Address, request.Phone,
            request.Email, request.Website, request.Description, request.LogoPath), ct));

    /// <summary>Turns company mode on.</summary>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(CancellationToken ct) =>
        Ok(await _profiles.ActivateAsync(CurrentUserId, ct));

    /// <summary>Turns company mode off. Profile data and documents are kept, not deleted.</summary>
    [HttpPost("deactivate")]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(CancellationToken ct) =>
        Ok(await _profiles.DeactivateAsync(CurrentUserId, ct));

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

    /// <summary>Your company's supporting documents.</summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(CancellationToken ct) =>
        Ok(await _profiles.GetDocumentsAsync(CurrentUserId, ct));

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

    /// <summary>Attaches an uploaded document file to your company profile.</summary>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(CompanyDocumentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDocument([FromBody] AddCompanyDocumentApiRequest request, CancellationToken ct)
    {
        var document = await _profiles.AddDocumentAsync(CurrentUserId, new AddCompanyDocumentRequest(
            request.FileName, request.FilePath, request.FileSizeBytes), ct);
        return Created($"/api/v1/company-profile/documents/{document.Id}", document);
    }

    /// <summary>Removes a document from your company profile.</summary>
    [HttpDelete("documents/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(int id, CancellationToken ct)
    {
        await _profiles.DeleteDocumentAsync(CurrentUserId, id, ct);
        return NoContent();
    }

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
