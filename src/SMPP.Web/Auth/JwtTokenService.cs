using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;

namespace SMPP.Web.Auth;

/// <summary>
/// Signs the API's bearer tokens with the claims the rest of the app already reads: the user id
/// on <see cref="ClaimTypes.NameIdentifier"/> (what ICurrentUserService and Identity's
/// UserManager look for) and the role name on <see cref="ClaimTypes.Role"/>, so a JWT-authenticated
/// request and a cookie-authenticated one are indistinguishable to every service below the
/// controller.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken IssueForUser(ApplicationUser user) => Issue(user, _options.AccessTokenMinutes, "pwd");

    public AccessToken IssueForMachine(ApplicationUser user) => Issue(user, _options.MachineTokenMinutes, "api-key");

    private AccessToken Issue(ApplicationUser user, int lifetimeMinutes, string authMethod)
    {
        var expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes);
        var roleName = user.Role == UserRole.Superadmin ? RoleNames.Superadmin : RoleNames.Account;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Role, roleName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            // Lets an endpoint tell a password session from a machine one, should a future
            // endpoint want to be off-limits to API-credential callers.
            new("amr", authMethod),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
