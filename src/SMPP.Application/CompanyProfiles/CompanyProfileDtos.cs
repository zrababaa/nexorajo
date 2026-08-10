namespace SMPP.Application.CompanyProfiles;

public record CompanyProfileDto(
    int Id,
    int AccountId,
    bool IsActive,
    string? RegistrationId,
    string? CompanyName,
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? Description,
    string? LogoPath,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// <see cref="LogoPath"/> is the path returned by the logo-upload endpoint, passed back in on
/// save - the same decoupled upload-then-reference pattern as <c>Payment.ProofFilePath</c>.
/// </summary>
public record UpdateCompanyProfileRequest(
    string? RegistrationId,
    string? CompanyName,
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? Description,
    string? LogoPath);

public record CompanyDocumentDto(int Id, string FileName, string FilePath, long FileSizeBytes, DateTime CreatedAt);

/// <summary>Attaches a file already saved by the document-upload endpoint to the profile.</summary>
public record AddCompanyDocumentRequest(string FileName, string FilePath, long FileSizeBytes);
