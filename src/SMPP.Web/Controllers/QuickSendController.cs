using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Campaigns;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Web.ViewModels.Sending;

namespace SMPP.Web.Controllers;

public class QuickSendController : Controller
{
    private const int PageSize = 15;

    private readonly IQuickSendService _quickSendService;
    private readonly IHistoryService _historyService;
    private readonly ICampaignService _campaignService;
    private readonly ICampaignNumberParser _numberParser;
    private readonly ICurrentUserService _currentUser;

    public QuickSendController(
        IQuickSendService quickSendService,
        IHistoryService historyService,
        ICampaignService campaignService,
        ICampaignNumberParser numberParser,
        ICurrentUserService currentUser)
    {
        _quickSendService = quickSendService;
        _historyService = historyService;
        _campaignService = campaignService;
        _numberParser = numberParser;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var history = await _historyService.GetPagedAsync(_currentUser.UserId, _currentUser.Role, MessageSource.QuickSend, null, page, PageSize, ct);
        ViewBag.History = history;
        await LoadSavedListsAsync(ct);
        return View(new QuickSendViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(QuickSendViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var history = await _historyService.GetPagedAsync(_currentUser.UserId, _currentUser.Role, MessageSource.QuickSend, null, 1, PageSize, ct);
            ViewBag.History = history;
            await LoadSavedListsAsync(ct);
            return View(nameof(Index), model);
        }

        try
        {
            var summary = await _quickSendService.SubmitAsync(_currentUser.UserId, new QuickSendRequest(model.RawNumbers, model.Message, model.SenderId), ct);
            TempData["Success"] = $"Queued {summary.RecipientCount} message(s), cost {summary.TotalCost:0.####}. Remaining balance: {summary.RemainingBalance:0.####}.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Saves the numbers currently in the form as a reusable Campaign (recipient list), without
    /// sending anything. A dedicated action rather than a flag on Submit so the Message/Sender ID
    /// fields (irrelevant to just saving a list) don't need to pass validation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveList(QuickSendViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.SaveListName))
        {
            TempData["Error"] = "Enter a name for the list before saving it.";
            return RedirectToAction(nameof(Index));
        }

        var parsed = _numberParser.ParsePasted(model.RawNumbers ?? string.Empty);
        if (parsed.Count == 0)
        {
            TempData["Error"] = "No valid phone numbers were found in the input provided.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var code = $"QS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
            await _campaignService.CreateAsync(_currentUser.UserId, new CreateCampaignRequest(
                model.SaveListName.Trim(),
                code,
                parsed.NormalizedNumbers,
                CampaignSourceType.Pasted,
                null,
                null), ct);
            TempData["Success"] = $"Saved \"{model.SaveListName.Trim()}\" as a list with {parsed.Count} number(s).";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Returns the numbers for one of the current user's saved lists, for the "Use a saved list" picker to inject into the Numbers box.</summary>
    public async Task<IActionResult> CampaignNumbers(int id, CancellationToken ct)
    {
        var campaign = await _campaignService.GetByIdAsync(id, _currentUser.UserId, ct);
        if (campaign is null)
        {
            return NotFound();
        }

        return Json(new { numbers = campaign.Numbers });
    }

    private async Task LoadSavedListsAsync(CancellationToken ct)
    {
        var campaigns = await _campaignService.GetPagedAsync(_currentUser.UserId, 1, 500, ct);
        ViewBag.SavedLists = campaigns.Items;
    }
}
