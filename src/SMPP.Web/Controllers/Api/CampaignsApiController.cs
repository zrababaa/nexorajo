using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Campaigns;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateCampaignApiRequest
{
    [Required]
    [MaxLength(190)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Your own reference for the list. Generated for you when omitted.</summary>
    public string? ExternalCampaignCode { get; init; }

    /// <summary>Recipient numbers, separated by commas, spaces, or new lines. Deduplicated on save.</summary>
    [Required]
    public string Numbers { get; init; } = string.Empty;
}

public record UpdateCampaignApiRequest
{
    [Required]
    [MaxLength(190)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Numbers { get; init; } = string.Empty;
}

/// <summary>Multipart form for importing a recipient list from a CSV/TXT/XLSX file.</summary>
public class ImportCampaignApiRequest
{
    [Required]
    [MaxLength(190)]
    public string Name { get; set; } = string.Empty;

    public string? ExternalCampaignCode { get; set; }

    [Required]
    public IFormFile File { get; set; } = default!;
}

/// <summary>
/// Saved recipient lists ("campaigns"), the input to a bulk send. Every endpoint here is scoped
/// to the calling account - you only ever see, change, or delete your own lists, in every role.
/// </summary>
[Route("api/v1/campaigns")]
[Tags("Campaigns")]
public class CampaignsApiController : ApiControllerBase
{
    private readonly ICampaignService _campaigns;
    private readonly ICampaignNumberParser _numberParser;

    public CampaignsApiController(ICampaignService campaigns, ICampaignNumberParser numberParser)
    {
        _campaigns = campaigns;
        _numberParser = numberParser;
    }

    /// <summary>Lists your saved recipient lists, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CampaignListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _campaigns.GetPagedAsync(CurrentUserId, Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>One list, including its full comma-separated number set.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var campaign = await _campaigns.GetByIdAsync(id, CurrentUserId, ct);
        return campaign is null ? NotFound(new ApiErrorResponse("Campaign not found.")) : Ok(campaign);
    }

    /// <summary>Creates a list from numbers sent inline.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCampaignApiRequest request, CancellationToken ct)
    {
        var parsed = _numberParser.ParsePasted(request.Numbers);
        if (parsed.Count == 0)
        {
            return BadRequest(new ApiErrorResponse("No valid phone numbers were found in the input provided."));
        }

        return await CreateAsync(request.Name, request.ExternalCampaignCode, parsed, CampaignSourceType.Pasted, ct);
    }

    /// <summary>Creates a list by uploading a CSV, TXT, or XLSX file of numbers.</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Import([FromForm] ImportCampaignApiRequest request, CancellationToken ct)
    {
        if (request.File.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("The uploaded file is empty."));
        }

        await using var stream = request.File.OpenReadStream();
        var parsed = _numberParser.ParseCsv(stream);
        if (parsed.Count == 0)
        {
            return BadRequest(new ApiErrorResponse("No valid phone numbers were found in the uploaded file."));
        }

        return await CreateAsync(request.Name, request.ExternalCampaignCode, parsed, CampaignSourceType.CsvUpload, ct);
    }

    /// <summary>Renames a list and replaces its numbers.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCampaignApiRequest request, CancellationToken ct)
    {
        var parsed = _numberParser.ParsePasted(request.Numbers);
        if (parsed.Count == 0)
        {
            return BadRequest(new ApiErrorResponse("No valid phone numbers were found in the input provided."));
        }

        await _campaigns.UpdateAsync(id, CurrentUserId, new UpdateCampaignRequest(request.Name, parsed.NormalizedNumbers), ct);
        return Ok(await _campaigns.GetByIdAsync(id, CurrentUserId, ct));
    }

    /// <summary>Deletes one of your lists. Messages already sent to it are untouched.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _campaigns.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    private async Task<IActionResult> CreateAsync(
        string name, string? code, NumberListResult parsed, CampaignSourceType sourceType, CancellationToken ct)
    {
        // The code is the caller's own reference; the UI generates one when the user doesn't
        // supply it, and the API does the same rather than making it a required field.
        code = string.IsNullOrWhiteSpace(code)
            ? $"API{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}"
            : code.Trim();

        var id = await _campaigns.CreateAsync(CurrentUserId, new CreateCampaignRequest(
            name.Trim(), code, parsed.NormalizedNumbers, sourceType), ct);

        return CreatedAtAction(nameof(GetById), new { id }, await _campaigns.GetByIdAsync(id, CurrentUserId, ct));
    }
}
