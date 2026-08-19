using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.SmsTemplates;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateSmsTemplateApiRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    /// <summary>The SMS text, with <c>[Placeholder]</c> tokens for whatever varies (e.g. "Hello [Name], please come at this [Date]").</summary>
    [Required]
    public string Body { get; init; } = string.Empty;
}

public record UpdateSmsTemplateApiRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Body { get; init; } = string.Empty;
}

/// <summary>
/// Reusable, placeholder-driven SMS bodies ("SMS Templates"), the input to a personalized Bulk
/// Send or Scheduled Send alongside a Campaign. Every endpoint here is scoped to the calling
/// account - you only ever see, change, or delete your own templates, in every role.
///
/// A template's <c>[Name]</c>/<c>[CompanyName]</c>/<c>[Email]</c>/<c>[Phone]</c>/<c>[Address]</c>
/// placeholders are filled per recipient from the account's Customers (matched by phone number)
/// when sent; any other placeholder (e.g. <c>[Date]</c>) must be supplied as a value at send time
/// via <c>templateVariables</c> on the Bulk Send/Scheduled Send request.
/// </summary>
[Route("api/v1/sms-templates")]
[Tags("SMS Templates")]
public class SmsTemplatesApiController : ApiControllerBase
{
    private readonly ISmsTemplateService _templates;

    public SmsTemplatesApiController(ISmsTemplateService templates)
    {
        _templates = templates;
    }

    /// <summary>Lists your saved templates, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SmsTemplateListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _templates.GetPagedAsync(CurrentUserId, Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>One template, including its full body and the placeholders found in it.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SmsTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var template = await _templates.GetByIdAsync(id, CurrentUserId, ct);
        return template is null ? NotFound(new ApiErrorResponse("SMS template not found.")) : Ok(template);
    }

    /// <summary>Creates a template.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SmsTemplateDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSmsTemplateApiRequest request, CancellationToken ct)
    {
        var id = await _templates.CreateAsync(CurrentUserId, new CreateSmsTemplateRequest(request.Name.Trim(), request.Body), ct);
        return CreatedAtAction(nameof(GetById), new { id }, await _templates.GetByIdAsync(id, CurrentUserId, ct));
    }

    /// <summary>Renames a template and replaces its body.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SmsTemplateDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSmsTemplateApiRequest request, CancellationToken ct)
    {
        await _templates.UpdateAsync(id, CurrentUserId, new UpdateSmsTemplateRequest(request.Name.Trim(), request.Body), ct);
        return Ok(await _templates.GetByIdAsync(id, CurrentUserId, ct));
    }

    /// <summary>
    /// Deletes one of your templates. If a still-pending scheduled send was created from it, that
    /// send will fail (not silently send stale content) when it fires - same as deleting a
    /// Campaign a scheduled send still points to.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _templates.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}
