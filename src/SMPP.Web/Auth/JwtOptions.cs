namespace SMPP.Web.Auth;

/// <summary>
/// Signing/validation settings for the REST API's bearer tokens, bound from the "Jwt" section.
/// <see cref="Key"/> has no default on purpose: startup fails loudly rather than signing
/// production tokens with a well-known secret.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "smpp-api";

    public string Audience { get; set; } = "smpp-api";

    /// <summary>HMAC-SHA256 signing key; must be at least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Lifetime of a token issued to a human signing in with a password.</summary>
    public int AccessTokenMinutes { get; set; } = 480;

    /// <summary>
    /// Lifetime of a token issued to a machine client exchanging its API token/secret. Shorter,
    /// because the client can mint a new one at any time without user interaction.
    /// </summary>
    public int MachineTokenMinutes { get; set; } = 60;
}
