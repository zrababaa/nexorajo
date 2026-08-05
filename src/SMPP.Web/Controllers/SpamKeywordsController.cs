using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.SpamKeywords;
using SMPP.Web.ViewModels.SpamKeywords;

namespace SMPP.Web.Controllers;

[Authorize(Roles = "Superadmin")]
public class SpamKeywordsController : Controller
{
    private const int BlockedAttemptsPageSize = 20;
    private const int KeywordsPageSize = 20;

    private readonly ISpamKeywordService _spamKeywordService;
    private readonly ICurrentUserService _currentUser;

    public SpamKeywordsController(ISpamKeywordService spamKeywordService, ICurrentUserService currentUser)
    {
        _spamKeywordService = spamKeywordService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, int keywordsPage = 1, CancellationToken ct = default)
    {
        var model = new SpamKeywordsPageViewModel
        {
            Keywords = await _spamKeywordService.GetPagedAsync(keywordsPage, KeywordsPageSize, ct),
            BlockedAttempts = await _spamKeywordService.GetBlockedAttemptsAsync(page, BlockedAttemptsPageSize, ct),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSpamKeywordViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            await _spamKeywordService.CreateAsync(_currentUser.UserId, new CreateSpamKeywordRequest(model.Keyword, model.KeywordType), ct);
            TempData["Success"] = "Keyword added.";
        }
        else
        {
            TempData["Error"] = "Please provide a valid keyword.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _spamKeywordService.DeleteAsync(id, ct);
        TempData["Success"] = "Keyword deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEnabled(int id, bool isEnabled, CancellationToken ct)
    {
        await _spamKeywordService.SetEnabledAsync(id, isEnabled, ct);
        TempData["Success"] = isEnabled ? "Keyword enabled." : "Keyword disabled.";
        return RedirectToAction(nameof(Index));
    }
}
