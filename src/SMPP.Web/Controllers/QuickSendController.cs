using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
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
    private readonly ICurrentUserService _currentUser;

    public QuickSendController(IQuickSendService quickSendService, IHistoryService historyService, ICurrentUserService currentUser)
    {
        _quickSendService = quickSendService;
        _historyService = historyService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var history = await _historyService.GetPagedAsync(_currentUser.UserId, _currentUser.Role, MessageSource.QuickSend, null, page, PageSize, ct);
        ViewBag.History = history;
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
}
