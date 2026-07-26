using Microsoft.AspNetCore.Identity;
using SMPP.Domain.Enums;

namespace SMPP.Infrastructure.Identity;

/// <summary>
/// Extends ASP.NET Core Identity's user directly with the app's business fields, rather than
/// a separate 1:1 profile table, since nearly every legacy business field is auth-adjacent.
/// Replaces legacy's ad hoc $fillable-only "users" table (user_role, otp_status/get_otp -
/// dropped entirely, package_id-as-name-string - fixed to a real FK here, creator_id/creater_id
/// spelling split - normalized to CreatedByUserId).
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public int? PackageId { get; set; }
    public decimal Balance { get; set; }
    public string? SmsSenderId { get; set; }
    public string? FreeSenderNumber { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    /// <summary>Self-referential ownership chain: Superadmin -&gt; WhiteLabelAdmin -&gt; EndUser.</summary>
    public int? CreatedByUserId { get; set; }

    public string? WhiteLabelDomain { get; set; }
    public string? WhiteLabelLogoPath { get; set; }

    public string? ApiToken { get; set; }
    public string? ApiSecret { get; set; }
    public string? ApiIpAllowlist { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
