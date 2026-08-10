using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>A file (registration certificate, etc.) attached to an Account's <see cref="CompanyProfile"/>.</summary>
public class CompanyDocument : AuditableEntity
{
    public int CompanyProfileId { get; set; }
    public int AccountId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
