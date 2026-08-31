using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.UrlShortening;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record ShortenUrlApiRequest
{
    /// <summary>The absolute http(s) URL to shorten.</summary>
    [Required]
    public string Url { get; init; } = string.Empty;
}

public record ShortenUrlApiResponse(string ShortUrl);

/// <summary>
/// Shortens a single URL through the configured provider (Short.io). The Quick Send and Bulk Send
/// screens shorten tracking links inline when "Shorten the link" is selected; this exposes the
/// same capability on its own for ad-hoc use.
///
/// Kept on the versioned <c>/api/v1</c> surface for consistency with every other endpoint (Swagger
/// server URL, CORS, the Angular dev proxy all assume that prefix).
/// </summary>
[Route("api/v1/url-shortener")]
[Tags("Link Tracking")]
public class UrlShortenerApiController : ApiControllerBase
{
    private readonly IUrlShortenerService _shortener;

    public UrlShortenerApiController(IUrlShortenerService shortener)
    {
        _shortener = shortener;
    }

    /// <summary>Returns a shortened URL for the supplied link.</summary>
    /// <response code="400">The URL was missing or invalid, or the shortener is not configured or refused the request.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ShortenUrlApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Shorten([FromBody] ShortenUrlApiRequest request, CancellationToken ct)
    {
        var shortUrl = await _shortener.ShortenUrlAsync(request.Url, ct);
        return Ok(new ShortenUrlApiResponse(shortUrl));
    }
}
