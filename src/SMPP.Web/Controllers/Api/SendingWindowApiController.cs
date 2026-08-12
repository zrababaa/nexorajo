using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.SendingWindow;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record SendingWindowApiResponse(bool IsEnabled, TimeOnly StartTime, TimeOnly EndTime);

public record SetSendingWindowApiRequest
{
    public bool IsEnabled { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}

/// <summary>
/// The daily time-of-day window Bulk Send is restricted to (server local time). Superadmin only -
/// enforced against every Bulk Send, immediate or scheduled, in SendCore.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
[Route("api/v1/sending-window")]
[Tags("Sending window")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
public class SendingWindowApiController : ApiControllerBase
{
    private readonly ISendingWindowService _sendingWindow;

    public SendingWindowApiController(ISendingWindowService sendingWindow)
    {
        _sendingWindow = sendingWindow;
    }

    /// <summary>The current window. When disabled, Bulk Send is unrestricted.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SendingWindowApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var window = await _sendingWindow.GetAsync(ct);
        return Ok(new SendingWindowApiResponse(window.IsEnabled, window.StartTime, window.EndTime));
    }

    /// <summary>Updates the window. A window where StartTime is after EndTime wraps past midnight.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SendingWindowApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Set([FromBody] SetSendingWindowApiRequest request, CancellationToken ct)
    {
        var window = await _sendingWindow.SetAsync(CurrentUserId, request.IsEnabled, request.StartTime, request.EndTime, ct);
        return Ok(new SendingWindowApiResponse(window.IsEnabled, window.StartTime, window.EndTime));
    }
}
