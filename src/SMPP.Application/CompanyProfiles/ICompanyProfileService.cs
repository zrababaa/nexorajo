namespace SMPP.Application.CompanyProfiles;

/// <summary>
/// The self-service company profile an Account maintains about itself: registration details,
/// logo, and supporting documents. Created lazily (get-or-create) on first access - there is no
/// Superadmin approval step, the Account controls <see cref="CompanyProfileDto.IsActive"/> itself.
/// </summary>
public interface ICompanyProfileService
{
    Task<CompanyProfileDto> GetAsync(int accountUserId, CancellationToken ct = default);

    Task<CompanyProfileDto> ActivateAsync(int accountUserId, CancellationToken ct = default);

    Task<CompanyProfileDto> DeactivateAsync(int accountUserId, CancellationToken ct = default);

    Task<CompanyProfileDto> UpdateAsync(int accountUserId, UpdateCompanyProfileRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CompanyDocumentDto>> GetDocumentsAsync(int accountUserId, CancellationToken ct = default);

    Task<CompanyDocumentDto> AddDocumentAsync(int accountUserId, AddCompanyDocumentRequest request, CancellationToken ct = default);

    Task DeleteDocumentAsync(int accountUserId, int documentId, CancellationToken ct = default);
}
