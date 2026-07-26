using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SMPP.Infrastructure.Identity;

namespace SMPP.Web.Filters;

/// <summary>
/// Replaces legacy's CheckBlacklist middleware. Blocks access for EndUser accounts that are
/// inactive or whose package validity window (DateFrom/DateTo) has elapsed. Superadmin and
/// WhiteLabelAdmin accounts are never blocked here (mirrors legacy's role-2-only scoping).
/// No OTP step - that legacy gate was non-functional and has been dropped entirely.
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
            if (user is not null && user.Role == Domain.Enums.UserRole.EndUser)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var expired = user.DateTo.HasValue && user.DateTo.Value < today;
                var notYetStarted = user.DateFrom.HasValue && user.DateFrom.Value > today;

                if (!user.IsActive || expired || notYetStarted)
                {
                    await _signInManager.SignOutAsync();
                    context.Result = new RedirectToActionResult("Login", "Account", new { blocked = true });
                    return;
                }
            }
        }

        await next();
    }
}
