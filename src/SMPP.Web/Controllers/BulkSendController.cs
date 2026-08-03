using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Campaigns;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Sending;
using SMPP.Domain.Enums;
using SMPP.Web.ViewModels.Sending;

namespace SMPP.Web.Controllers;

public class BulkSendController : Controller
{
    private const int PageSize = 15;

    private readonly IBulkSendService _bulkSendService;
    private readonly IHistoryService _historyService;
    private readonly ICampaignService _campaignService;
    private readonly ISendPolicyService _sendPolicy;
    private readonly ICurrentUserService _currentUser;

    public BulkSendController(
        IBulkSendService bulkSendService,
        IHistoryService historyService,
        ICampaignService campaignService,
        ISendPolicyService sendPolicy,
        ICurrentUserService currentUser)
    {
        _bulkSendService = bulkSendService;
        _historyService = historyService;
        _campaignService = campaignService;
        _sendPolicy = sendPolicy;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        await LoadViewDataAsync(page, ct);
        return View(new BulkSendViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(BulkSendViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadViewDataAsync(1, ct);
            return View(nameof(Index), model);
        }

        try
        {
            var summary = await _bulkSendService.SubmitAsync(
                _currentUser.UserId, new BulkSendRequest(model.CampaignId, model.Message, model.EffectiveSenderId), ct);
            TempData["Success"] = $"Queued {summary.RecipientCount} message(s), cost {summary.TotalCost:0.####}. Remaining balance: {summary.RemainingBalance:0.####}.";
        }
        catch (SpamBlockedException ex)
        {
            // Redisplayed rather than redirected so the user gets the offending words in a dialog
            // with the message they wrote still in the box.
            ViewBag.SpamBlockedTerms = ex.MatchedTerms;
            await LoadViewDataAsync(1, ct);
            return View(nameof(Index), model);
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadViewDataAsync(int page, CancellationToken ct)
    {
        ViewData["SendPolicy"] = await _sendPolicy.GetAsync(_currentUser.UserId, ct);

        var campaigns = await _campaignService.GetPagedAsync(_currentUser.UserId, 1, 500, ct);
        ViewBag.Campaigns = campaigns.Items;

        var history = await _historyService.GetPagedAsync(_currentUser.UserId, _currentUser.Role, MessageSource.BulkSend, null, page, PageSize, ct);
        ViewBag.History = history;
    }
}
