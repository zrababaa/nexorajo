using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Filters;

/// <summary>
/// Replaces legacy's CheckBlacklist middleware. Blocks access for Account users that are
/// inactive or whose validity window (DateFrom/DateTo) has elapsed. Superadmin is never
/// blocked here. No OTP step - that legacy gate was non-functional and has been dropped.
///
/// A blocked browser session is signed out and sent to the login screen; a blocked API call
/// gets a 403 with the same explanation, because there is no session to end and nowhere to
/// redirect a bearer-token client to.
/// </summary>
public class CheckBlacklistFilter : IAsyncActionFilter
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public CheckBlacklistFilter(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            if (user is not null && user.Role == Domain.Enums.UserRole.Account)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var expired = user.DateTo.HasValue && user.DateTo.Value < today;
                var notYetStarted = user.DateFrom.HasValue && user.DateFrom.Value > today;

                if (!user.IsActive || expired || notYetStarted)
                {
                    const string message = "Your account is inactive or has expired. Contact your administrator.";

                    if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Result = new ObjectResult(new ApiErrorResponse(message))
                        {
                            StatusCode = StatusCodes.Status403Forbidden,
                        };
                        return;
                    }

                    await _signInManager.SignOutAsync();
                    context.Result = new RedirectToActionResult("Login", "Account", new { blocked = true });
                    return;
                }
            }
        }

        await next();
    }
}
