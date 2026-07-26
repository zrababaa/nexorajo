using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.PublicApi;

namespace SMPP.Web.Controllers.Api;

public record SendMessageApiRequest(string Numbers, string Message, string? SenderId);

/// <summary>
/// Public send API. Ports legacy's SendMessageApi::sendMessage contract: auth via the
/// token-id/secret-key headers (an account's regenerable API credentials, managed by a
/// Superadmin under Accounts), same balance/segment/spam-filter pipeline as Quick Send.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api")]
public class SendController : ControllerBase
{
    private readonly IPublicApiAuthenticator _authenticator;
    private readonly IPublicApiSendService _sendService;

    public SendController(IPublicApiAuthenticator authenticator, IPublicApiSendService sendService)
    {
        _authenticator = authenticator;
        _sendService = sendService;
    }

    [HttpPost("send-message-api")]
    public async Task<IActionResult> SendMessageApi([FromBody] SendMessageApiRequest request, CancellationToken ct)
    {
        var token = Request.Headers["token-id"].ToString();
        var secret = Request.Headers["secret-key"].ToString();

        var userId = await _authenticator.AuthenticateAsync(token, secret, ct);
        if (userId is null)
        {
            return Unauthorized(new { status = "error", message = "Invalid token-id/secret-key." });
        }

        try
        {
            var summary = await _sendService.SubmitAsync(userId.Value, new PublicApiSendRequest(request.Numbers, request.Message, request.SenderId), ct);
            return Ok(new
            {
                status = "success",
                batchId = summary.BatchId,
                totalMessages = summary.RecipientCount,
                segmentsPerMessage = summary.SegmentsPerMessage,
                totalCharge = summary.TotalCost,
                remainingBalance = summary.RemainingBalance,
            });
        }
        catch (AppException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }
}
