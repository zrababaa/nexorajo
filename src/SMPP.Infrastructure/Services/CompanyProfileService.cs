using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.CompanyProfiles;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class CompanyProfileService : ICompanyProfileService
{
    private readonly SmppDbContext _db;

    public CompanyProfileService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<CompanyProfileDto> GetAsync(int accountUserId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateAsync(accountUserId, ct);
        return ToDto(profile);
    }

    public async Task<CompanyProfileDto> ActivateAsync(int accountUserId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateAsync(accountUserId, ct);
        profile.IsActive = true;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    public async Task<CompanyProfileDto> DeactivateAsync(int accountUserId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateAsync(accountUserId, ct);
        profile.IsActive = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    public async Task<CompanyProfileDto> UpdateAsync(int accountUserId, UpdateCompanyProfileRequest request, CancellationToken ct = default)
    {
        var profile = await GetOrCreateAsync(accountUserId, ct);

        profile.RegistrationId = request.RegistrationId;
        profile.CompanyName = request.CompanyName;
        profile.Address = request.Address;
        profile.Phone = request.Phone;
        profile.Email = request.Email;
        profile.Website = request.Website;
        profile.Description = request.Description;
        profile.LogoPath = request.LogoPath;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    public async Task<IReadOnlyList<CompanyDocumentDto>> GetDocumentsAsync(int accountUserId, CancellationToken ct = default) =>
        await _db.CompanyDocuments
            .AsNoTracking()
            .Where(d => d.AccountId == accountUserId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new CompanyDocumentDto(d.Id, d.FileName, d.FilePath, d.FileSizeBytes, d.CreatedAt))
            .ToListAsync(ct);

    public async Task<CompanyDocumentDto> AddDocumentAsync(int accountUserId, AddCompanyDocumentRequest request, CancellationToken ct = default)
    {
        var profile = await GetOrCreateAsync(accountUserId, ct);

        var document = new CompanyDocument
        {
            CompanyProfileId = profile.Id,
            AccountId = accountUserId,
            FileName = request.FileName,
            FilePath = request.FilePath,
            FileSizeBytes = request.FileSizeBytes,
        };

        _db.CompanyDocuments.Add(document);
        await _db.SaveChangesAsync(ct);

        return new CompanyDocumentDto(document.Id, document.FileName, document.FilePath, document.FileSizeBytes, document.CreatedAt);
    }

    public async Task DeleteDocumentAsync(int accountUserId, int documentId, CancellationToken ct = default)
    {
        var document = await _db.CompanyDocuments.SingleOrDefaultAsync(d => d.Id == documentId && d.AccountId == accountUserId, ct)
            ?? throw new AppException("Document not found.");

        _db.CompanyDocuments.Remove(document);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CompanyProfile> GetOrCreateAsync(int accountUserId, CancellationToken ct)
    {
        var profile = await _db.CompanyProfiles.SingleOrDefaultAsync(p => p.AccountId == accountUserId, ct);
        if (profile is not null)
        {
            return profile;
        }

        profile = new CompanyProfile { AccountId = accountUserId, IsActive = false };
        _db.CompanyProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    private static CompanyProfileDto ToDto(CompanyProfile p) => new(
        p.Id, p.AccountId, p.IsActive, p.RegistrationId, p.CompanyName, p.Address, p.Phone, p.Email,
        p.Website, p.Description, p.LogoPath, p.CreatedAt, p.UpdatedAt);
}
