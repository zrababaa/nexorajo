using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Web.ViewModels.Accounts;

namespace SMPP.Web.Controllers;

/// <summary>Superadmin-only: create/manage Accounts and their credits (2-tier model).</summary>
[Authorize(Roles = "Superadmin")]
public class AccountsController : Controller
{
    private const int PageSize = 15;

    private readonly IAccountService _accountService;
    private readonly IBalanceLedgerService _ledger;
    private readonly ICurrentUserService _currentUser;

    public AccountsController(IAccountService accountService, IBalanceLedgerService ledger, ICurrentUserService currentUser)
    {
        _accountService = accountService;
        _ledger = ledger;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _accountService.GetPagedAsync(page, PageSize, ct);
        return View(result);
    }

    public IActionResult Create() => View(new CreateAccountViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _accountService.CreateAsync(_currentUser.UserId, new CreateAccountRequest(
                model.Username, model.Email, model.FullName, model.MobileNo, model.Password,
                model.InitialBalance, model.SenderIds, model.AllowFreeSenderId), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var account = await _accountService.GetByIdAsync(id, ct);
        if (account is null)
        {
            return NotFound();
        }

        return View(new EditAccountViewModel
        {
            Id = account.Id,
            Username = account.Username,
            Email = account.Email,
            FullName = account.FullName,
            MobileNo = account.MobileNo,
            IsActive = account.IsActive,
            SenderIds = account.SenderIds,
            AllowFreeSenderId = account.AllowFreeSenderId,
            DateFrom = account.DateFrom,
            DateTo = account.DateTo,
            Balance = account.Balance,
            ApiToken = account.ApiToken,
            ApiSecret = account.ApiSecret,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditAccountViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _accountService.UpdateAsync(id, new UpdateAccountRequest(
                model.FullName, model.MobileNo, model.IsActive, model.SenderIds,
                model.AllowFreeSenderId, model.DateFrom, model.DateTo), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Credit(AdjustBalanceViewModel model, CancellationToken ct)
    {
        try
        {
            await _ledger.ApplyAsync(new BalanceLedgerRequest(
                model.AccountId, _currentUser.UserId, model.Amount, TransactionKind.Credit, TransactionSource.ManualAdjustment), ct);
            TempData["Success"] = $"Credited {model.Amount:0.####} successfully.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = model.AccountId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Debit(AdjustBalanceViewModel model, CancellationToken ct)
    {
        try
        {
            await _ledger.ApplyAsync(new BalanceLedgerRequest(
                model.AccountId, _currentUser.UserId, model.Amount, TransactionKind.Debit, TransactionSource.ManualAdjustment), ct);
            TempData["Success"] = $"Debited {model.Amount:0.####} successfully.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = model.AccountId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateApiCredentials(int id, CancellationToken ct)
    {
        await _accountService.RegenerateApiCredentialsAsync(id, ct);
        TempData["Success"] = "API credentials regenerated.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}
