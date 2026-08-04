using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.AdminBudget;
using SMPP.Application.Common;
using SMPP.Infrastructure.Identity;
using SMPP.Web.ViewModels.AdminBudget;

namespace SMPP.Web.Controllers;

/// <summary>
/// Superadmin-only: the shared pool of credits that can be handed out to accounts. See
/// IAdminBudgetService - every account credit draws from this pool.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin)]
public class AdminBudgetController : Controller
{
    private const int PageSize = 20;

    private readonly IAdminBudgetService _adminBudget;
    private readonly ICurrentUserService _currentUser;

    public AdminBudgetController(IAdminBudgetService adminBudget, ICurrentUserService currentUser)
    {
        _adminBudget = adminBudget;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var balance = await _adminBudget.GetBalanceAsync(ct);
        var log = await _adminBudget.GetLogAsync(page, PageSize, ct);

        return View(new AdminBudgetPageViewModel
        {
            Balance = balance,
            Log = log,
            Form = new SetAdminBudgetViewModel { NewBalance = balance },
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBalance(SetAdminBudgetViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The budget cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _adminBudget.SetBalanceAsync(_currentUser.UserId, model.NewBalance, model.Note, ct);
            TempData["Success"] = "Admin budget updated.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
