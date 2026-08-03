using Microsoft.AspNetCore.Identity;
using SMPP.Domain.Enums;

namespace SMPP.Infrastructure.Identity;

/// <summary>
/// Extends ASP.NET Core Identity's user directly with the app's business fields. Identity's
/// built-in UserName is the login username (distinct from Email - sign-in accepts either).
/// 2-tier model: Superadmin creates and funds Account users directly, no reseller tier.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal Balance { get; set; }
    public decimal RatePerMessage { get; set; }

    /// <summary>
    /// Comma-separated list of the Sender IDs this account may send under, chosen from a
    /// dropdown on the send screens. Ignored for Superadmin, which is unrestricted.
    /// </summary>
    public string? SenderIds { get; set; }

    /// <summary>
    /// When set, the account may also type a Sender ID of its own instead of picking from
    /// <see cref="SenderIds"/> (legacy's <c>free_sender</c> flag).
    /// </summary>
    public bool AllowFreeSenderId { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    /// <summary>Which Superadmin created this Account.</summary>
    public int? CreatedByUserId { get; set; }

    public string? ApiToken { get; set; }
    public string? ApiSecret { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
