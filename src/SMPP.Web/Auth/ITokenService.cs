using SMPP.Infrastructure.Identity;

namespace SMPP.Web.Auth;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Issues the bearer tokens the REST API authenticates with.</summary>
public interface ITokenService
{
    /// <summary>Token for an interactive sign-in (username/email + password).</summary>
    AccessToken IssueForUser(ApplicationUser user);

    /// <summary>Shorter-lived token for a machine client that presented its API token/secret pair.</summary>
    AccessToken IssueForMachine(ApplicationUser user);
}
