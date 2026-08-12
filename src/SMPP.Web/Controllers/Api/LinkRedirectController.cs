using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Web.Controllers.Api;

/// <summary>
/// The public tracking-link redirect: GET /l/{token} logs a click and 302-redirects to the
/// real destination LinkTrackingService substituted the token for. Anonymous by design - this
/// is the URL a recipient's browser hits straight off an SMS, it never carries credentials.
/// Matched by app.MapControllers() ahead of the Angular SPA fallback, same as
/// DeliveryStatusController's /api/getStatus.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("l")]
[Tags("Link Tracking")]
public class LinkRedirectController : ControllerBase
{
    private readonly SmppDbContext _db;

    public LinkRedirectController(SmppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Redirect(string token, CancellationToken ct)
    {
        var link = await _db.TrackedLinks.AsNoTracking().FirstOrDefaultAsync(t => t.Token == token, ct);
        if (link is null)
        {
            return NotFound();
        }

        var clickedAt = DateTime.UtcNow;

        _db.LinkClicks.Add(new LinkClick
        {
            TrackedLinkId = link.Id,
            BatchId = link.BatchId,
            ClickedAt = clickedAt,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
        });
        await _db.SaveChangesAsync(ct);

        await _db.TrackedLinks
            .Where(t => t.Id == link.Id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.ClickCount, t => t.ClickCount + 1)
                .SetProperty(t => t.FirstClickedAt, t => t.FirstClickedAt ?? clickedAt)
                .SetProperty(t => t.LastClickedAt, clickedAt), ct);

        // A plain 302, never a permanent redirect: a 301/308 would let the browser cache the
        // redirect and skip logging every click after the first.
        return Redirect(link.DestinationUrl);
    }
}
