using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>An end-customer an Account tracks in its own CRM. Always scoped to one owning Account.</summary>
public class Customer : AuditableEntity
{
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;
}
