using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.SpamKeywords;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateSpamKeywordApiRequest
{
    [Required]
    [MaxLength(190)]
    public string Keyword { get; init; } = string.Empty;

    /// <summary>Include (block on match), Exclude (allow past a broader rule), or Url.</summary>
    public SpamKeywordType KeywordType { get; init; }
}

/// <summary>
/// The content filter's word and URL list, applied to every send on every channel. Superadmin
/// only - an account cannot inspect or change what blocks its messages.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
[Route("api/v1/spam-keywords")]
[Tags("Content filter")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
public class SpamKeywordsApiController : ApiControllerBase
{
    private readonly ISpamKeywordService _keywords;

    public SpamKeywordsApiController(ISpamKeywordService keywords)
    {
        _keywords = keywords;
    }

    /// <summary>Every configured keyword and URL rule.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SpamKeywordListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _keywords.GetAllAsync(ct));

    /// <summary>Adds a rule. It takes effect on the next send.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SpamKeywordListItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSpamKeywordApiRequest request, CancellationToken ct)
    {
        var id = await _keywords.CreateAsync(CurrentUserId, new CreateSpamKeywordRequest(request.Keyword, request.KeywordType), ct);

        var created = (await _keywords.GetAllAsync(ct)).FirstOrDefault(k => k.Id == id);
        return Created($"/api/v1/spam-keywords/{id}", created);
    }

    /// <summary>Removes a rule.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _keywords.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Enables or disables a rule without deleting it.</summary>
    [HttpPatch("{id:int}/enabled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetEnabled(int id, [FromQuery] bool isEnabled, CancellationToken ct)
    {
        await _keywords.SetEnabledAsync(id, isEnabled, ct);
        return NoContent();
    }
}
