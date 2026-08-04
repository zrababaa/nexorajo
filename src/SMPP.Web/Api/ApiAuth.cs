namespace SMPP.Web.Api;

public static class ApiAuth
{
    /// <summary>
    /// Every <c>/api/v1</c> endpoint accepts either a bearer token (the normal way for an
    /// integration) or the app's own sign-in cookie, so the Swagger page and any in-browser call
    /// from the web UI work for a user who is already signed in.
    ///
    /// Spelled out rather than composed from JwtBearerDefaults/IdentityConstants because an
    /// attribute argument has to be a compile-time constant and those are static readonly
    /// fields. <see cref="AssertSchemeNamesMatchFramework"/> keeps the literals honest.
    /// </summary>
    public const string Schemes = "Bearer,Identity.Application";

    /// <summary>
    /// Fails fast at startup if a framework upgrade ever renames a scheme out from under the
    /// constant above - the endpoints would otherwise silently refuse every request.
    /// </summary>
    public static void AssertSchemeNamesMatchFramework()
    {
        var expected = $"{Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme}," +
                       $"{Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme}";

        if (Schemes != expected)
        {
            throw new InvalidOperationException(
                $"Authentication scheme names have changed: expected '{expected}', ApiAuth.Schemes is '{Schemes}'.");
        }
    }
}
