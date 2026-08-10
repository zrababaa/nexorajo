using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// The one company profile an Account may maintain about itself. Created lazily (get-or-create)
/// on first access rather than seeded, so <see cref="IsActive"/> - the self-service "company mode"
/// toggle - can start false with every other field empty.
/// </summary>
public class CompanyProfile : AuditableEntity
{
    public int AccountId { get; set; }
    public bool IsActive { get; set; }
    public string? RegistrationId { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? LogoPath { get; set; }
}
