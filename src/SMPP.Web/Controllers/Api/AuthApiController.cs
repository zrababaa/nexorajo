using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SMPP.Application.PublicApi;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;
using SMPP.Web.Auth;

namespace SMPP.Web.Controllers.Api;

public record LoginApiRequest
{
    /// <summary>Username or email address.</summary>
    [Required]
    public string Identifier { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Exchanges an account's API token/secret pair for a bearer token - no password involved.</summary>
public record MachineTokenApiRequest
{
    [Required]
    public string TokenId { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;
}

public record ChangePasswordApiRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}

public record ForgotPasswordApiRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordApiRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>The token from the reset link.</summary>
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}

public record AuthenticatedUserResponse(
    int Id,
    string Username,
    string Email,
    string FullName,
    string? MobileNo,
    UserRole Role,
    bool IsActive,
    decimal Balance,
    string? SenderIds,
    bool AllowFreeSenderId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? ApiToken,
    string? ApiSecret);

public record TokenApiResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    AuthenticatedUserResponse User);

/// <summary>
/// Sign-in for the REST API. Accounts are provisioned by a Superadmin (see
/// <see cref="AccountsApiController"/>), never self-registered, so there is no signup endpoint.
///
/// A human client posts credentials to <c>login</c>; a server-to-server integration posts its
/// API token/secret to <c>token</c>. Both return the same bearer token, sent back as
/// <c>Authorization: Bearer &lt;token&gt;</c> on every other endpoint.
///
/// Only the sign-in and recovery endpoints are open, and they say so individually: an
/// [AllowAnonymous] on the controller would open every action on it, including the two that
/// act on the caller's own account.
/// </summary>
[Route("api/v1/auth")]
[Tags("Authentication")]
public class AuthApiController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPublicApiAuthenticator _apiAuthenticator;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPublicApiAuthenticator apiAuthenticator,
        ITokenService tokens,
        ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _apiAuthenticator = apiAuthenticator;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Signs in with a username (or email) and password and returns a bearer token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Identifier)
            ?? await _userManager.FindByEmailAsync(request.Identifier);

        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse("Invalid username/email or password."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Unauthorized(new ApiErrorResponse("This account is temporarily locked out due to repeated failed attempts."));
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new ApiErrorResponse("Invalid username/email or password."));
        }

        if (IsBlocked(user))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiErrorResponse("Your account is inactive or has expired. Contact your administrator."));
        }

        return Ok(ToResponse(_tokens.IssueForUser(user), user));
    }

    /// <summary>
    /// Exchanges an account's API credentials (the same token-id/secret-key pair the legacy
    /// send endpoint takes) for a bearer token, so an integration can reach the whole API
    /// without storing a user's password.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MachineToken([FromBody] MachineTokenApiRequest request, CancellationToken ct)
    {
        var userId = await _apiAuthenticator.AuthenticateAsync(request.TokenId, request.SecretKey, ct);
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("Invalid token-id/secret-key."));
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse("Invalid token-id/secret-key."));
        }

        if (IsBlocked(user))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiErrorResponse("Your account is inactive or has expired. Contact your administrator."));
        }

        return Ok(ToResponse(_tokens.IssueForMachine(user), user));
    }

    /// <summary>The signed-in account: profile, balance, sender-ID policy, and its API credentials.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        return user is null ? Unauthorized(new ApiErrorResponse("Unknown user.")) : Ok(ToUserResponse(user));
    }

    /// <summary>Changes the signed-in account's password.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordApiRequest request)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse("Unknown user."));
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded ? NoContent() : BadRequest(ToError(result));
    }

    /// <summary>
    /// Starts password recovery. Always answers 202 whether or not the address exists, so the
    /// endpoint cannot be used to discover which emails are registered.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordApiRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Same stand-in as the MVC flow: no mail transport is wired yet, so the link is
            // logged. Swap both for an IEmailSender together.
            _logger.LogInformation("Password reset token for {Email}: {Token}", request.Email, encodedToken);
        }

        return Accepted();
    }

    /// <summary>Completes password recovery using the token from the reset link.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordApiRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Don't reveal whether the account exists.
            return NoContent();
        }

        var token = TryDecodeToken(request.Token);
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        return result.Succeeded ? NoContent() : BadRequest(ToError(result));
    }

    /// <summary>
    /// The reset link carries the token base64url-encoded; a client that already decoded it can
    /// post it raw, so both forms are accepted.
    /// </summary>
    private static string TryDecodeToken(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return token;
        }
    }

    private static bool IsBlocked(ApplicationUser user)
    {
        if (user.Role != UserRole.Account)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return !user.IsActive
            || (user.DateTo.HasValue && user.DateTo.Value < today)
            || (user.DateFrom.HasValue && user.DateFrom.Value > today);
    }

    private static ApiErrorResponse ToError(IdentityResult result) =>
        new(string.Join(" ", result.Errors.Select(e => e.Description)));

    private static TokenApiResponse ToResponse(AccessToken token, ApplicationUser user) =>
        new(token.Token, "Bearer", token.ExpiresAtUtc, ToUserResponse(user));

    private static AuthenticatedUserResponse ToUserResponse(ApplicationUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FullName, user.MobileNo,
            user.Role, user.IsActive, user.Balance, user.SenderIds, user.AllowFreeSenderId,
            user.DateFrom, user.DateTo, user.ApiToken, user.ApiSecret);
}
