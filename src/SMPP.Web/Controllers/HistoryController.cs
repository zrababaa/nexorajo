using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Enums;
using SMPP.Web.Services;
using SMPP.Web.ViewModels.History;

namespace SMPP.Web.Controllers;

/// <summary>
/// Unified, filterable message history report with CSV/Excel export - covers Quick Send,
/// Bulk Send, and Public API sends in one place, scoped via IHistoryService's role rules
/// (Account sees only its own rows, Superadmin sees everyone's).
/// </summary>
public class HistoryController : Controller
{
    private const int PageSize = 20;

    private readonly IHistoryService _historyService;
    private readonly IHistoryResendService _resendService;
    private readonly ICurrentUserService _currentUser;

    public HistoryController(IHistoryService historyService, IHistoryResendService resendService, ICurrentUserService currentUser)
    {
        _historyService = historyService;
        _resendService = resendService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        MessageSource? source, MessageStatus? status, string? campaignBatchId, string? receiver,
        DateOnly? dateFrom, DateOnly? dateTo, int page = 1, CancellationToken ct = default)
    {
        var filter = new HistoryFilterDto(source, status, campaignBatchId, receiver, dateFrom, dateTo);

        var pageResult = await _historyService.GetPagedAsync(_currentUser.UserId, _currentUser.Role, filter, page, PageSize, ct);
        var summary = await _historyService.GetSummaryAsync(_currentUser.UserId, _currentUser.Role, filter, ct);

        return View(new HistoryIndexViewModel
        {
            Page = pageResult,
            Summary = summary,
            Source = source,
            Status = status,
            CampaignBatchId = campaignBatchId,
            Receiver = receiver,
            DateFrom = dateFrom,
            DateTo = dateTo,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(int id, MessageSource source, CancellationToken ct)
    {
        try
        {
            var summary = await _resendService.ResendAsync(id, source, _currentUser.UserId, ct);
            TempData["Success"] = $"Message resent, cost {summary.TotalCost:0.####}. Remaining balance: {summary.RemainingBalance:0.####}.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportCsv(
        MessageSource? source, MessageStatus? status, string? campaignBatchId, string? receiver,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default)
    {
        var filter = new HistoryFilterDto(source, status, campaignBatchId, receiver, dateFrom, dateTo);
        var rows = await _historyService.GetForExportAsync(_currentUser.UserId, _currentUser.Role, filter, ct);
        var bytes = HistoryExportWriter.WriteCsv(rows);
        return File(bytes, "text/csv", $"history-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    public async Task<IActionResult> ExportExcel(
        MessageSource? source, MessageStatus? status, string? campaignBatchId, string? receiver,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default)
    {
        var filter = new HistoryFilterDto(source, status, campaignBatchId, receiver, dateFrom, dateTo);
        var rows = await _historyService.GetForExportAsync(_currentUser.UserId, _currentUser.Role, filter, ct);
        var bytes = HistoryExportWriter.WriteExcelHtml(rows);
        return File(bytes, "application/vnd.ms-excel", $"history-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xls");
    }
}
