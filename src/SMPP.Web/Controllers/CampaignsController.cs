using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Campaigns;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Web.ViewModels.Campaigns;

namespace SMPP.Web.Controllers;

public class CampaignsController : Controller
{
    private const int PageSize = 10;

    private readonly ICampaignService _campaignService;
    private readonly ICampaignNumberParser _numberParser;
    private readonly ICurrentUserService _currentUser;

    public CampaignsController(ICampaignService campaignService, ICampaignNumberParser numberParser, ICurrentUserService currentUser)
    {
        _campaignService = campaignService;
        _numberParser = numberParser;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _campaignService.GetPagedAsync(_currentUser.UserId, page, PageSize, ct);
        return View(result);
    }

    public IActionResult Create() => View(new CampaignFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignFormViewModel model, CancellationToken ct)
    {
        var parsed = ValidateAndParseNumbers(model);
        if (parsed is null)
        {
            return View(model);
        }

        try
        {
            await _campaignService.CreateAsync(_currentUser.UserId, new CreateCampaignRequest(
                model.Name,
                model.ExternalCampaignCode,
                parsed.NormalizedNumbers,
                model.CsvFile is not null ? CampaignSourceType.CsvUpload : CampaignSourceType.Pasted), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Campaign added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var campaign = await _campaignService.GetByIdAsync(id, _currentUser.UserId, ct);
        if (campaign is null)
        {
            return NotFound();
        }

        return View(new CampaignFormViewModel
        {
            Id = campaign.Id,
            Name = campaign.Name,
            ExternalCampaignCode = campaign.ExternalCampaignCode,
            PastedNumbers = campaign.Numbers,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CampaignFormViewModel model, CancellationToken ct)
    {
        var parsed = ValidateAndParseNumbers(model, isEdit: true);
        if (parsed is null)
        {
            return View(model);
        }

        try
        {
            await _campaignService.UpdateAsync(id, _currentUser.UserId, new UpdateCampaignRequest(
                model.Name,
                parsed.NormalizedNumbers), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Campaign updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _campaignService.DeleteAsync(id, _currentUser.UserId, ct);
            TempData["Success"] = "Campaign deleted successfully.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private NumberListResult? ValidateAndParseNumbers(CampaignFormViewModel model, bool isEdit = false)
    {
        var hasPasted = !string.IsNullOrWhiteSpace(model.PastedNumbers);
        var hasCsv = model.CsvFile is { Length: > 0 };

        if (hasPasted && hasCsv)
        {
            ModelState.AddModelError(string.Empty, "Please provide numbers either pasted or via CSV upload, not both.");
            return null;
        }

        if (!hasPasted && !hasCsv)
        {
            ModelState.AddModelError(string.Empty, isEdit
                ? "Please provide numbers either pasted or via CSV upload."
                : "Please upload your campaign data.");
            return null;
        }

        if (!ModelState.IsValid)
        {
            return null;
        }

        var parsed = hasCsv
            ? _numberParser.ParseCsv(model.CsvFile!.OpenReadStream())
            : _numberParser.ParsePasted(model.PastedNumbers!);

        if (parsed.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No valid phone numbers were found in the input provided.");
            return null;
        }

        return parsed;
    }
}
