using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Templates;
using SMPP.Web.ViewModels.Templates;

namespace SMPP.Web.Controllers;

public class TemplatesController : Controller
{
    private const int PageSize = 10;

    private readonly ITemplateService _templateService;
    private readonly ICurrentUserService _currentUser;

    public TemplatesController(ITemplateService templateService, ICurrentUserService currentUser)
    {
        _templateService = templateService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _templateService.GetPagedAsync(_currentUser.UserId, page, PageSize, ct);
        return View(result);
    }

    public IActionResult Create() => View(new TemplateFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TemplateFormViewModel model, CancellationToken ct)
    {
        if (model.CsvFile is not { Length: > 0 })
        {
            ModelState.AddModelError(nameof(model.CsvFile), "A recipient CSV file is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await using var stream = model.CsvFile!.OpenReadStream();
            await _templateService.CreateAsync(_currentUser.UserId, new CreateTemplateRequest(
                model.Name,
                model.TemplateCode,
                model.MessageBody,
                stream,
                model.CsvFile.FileName), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Template created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var template = await _templateService.GetByIdAsync(id, _currentUser.UserId, ct);
        if (template is null)
        {
            return NotFound();
        }

        return View(new TemplateFormViewModel
        {
            Id = template.Id,
            Name = template.Name,
            TemplateCode = template.TemplateCode,
            MessageBody = template.MessageBody,
            ExistingCsvFilePath = template.CsvFilePath,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TemplateFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _templateService.UpdateAsync(id, _currentUser.UserId, new UpdateTemplateRequest(model.Name, model.MessageBody), ct);
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Template updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _templateService.DeleteAsync(id, _currentUser.UserId, ct);
            TempData["Success"] = "Template deleted successfully.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
