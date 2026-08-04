using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Web.Controllers.Api;

/// <summary>
/// Delivery-receipt callback the external SMPP daemon calls as statuses come back from the
/// operator. Ports legacy's SendMessageApi::getStatus: same route, same query parameters, same
/// any-verb handling, and it still matches on the daemon-assigned get_message_id. Legacy had to
/// update two history tables here; this app has one.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api")]
[Tags("Legacy API")]
public class DeliveryStatusController : ControllerBase
{
    private static readonly Dictionary<string, MessageStatus> StatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PROCESS"] = MessageStatus.Processing,
        ["SENT"] = MessageStatus.Sent,
        ["DELIVRD"] = MessageStatus.Delivered,
        ["UNDELIV"] = MessageStatus.Undelivered,
        ["EXPIRED"] = MessageStatus.Expired,
        ["FAILED"] = MessageStatus.Failed,
    };

    private readonly SmppDbContext _db;

    public DeliveryStatusController(SmppDbContext db)
    {
        _db = db;
    }

    [HttpGet("getStatus")]
    [HttpPost("getStatus")]
    public async Task<IActionResult> GetStatus([FromQuery(Name = "message_id")] string? messageId, [FromQuery] string? status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new { status = "error", message = "message_id and status are required." });
        }

        if (!StatusCodes.TryGetValue(status, out var mapped))
        {
            return BadRequest(new { status = "error", message = $"Unrecognised status code '{status}'." });
        }

        // updated_at is left to MySQL's ON UPDATE CURRENT_TIMESTAMP rather than stamped here.
        // Legacy runs on Asia/Amman and its Eloquent update() writes local time, so a
        // DateTime.UtcNow from this app would land three hours behind every row the daemon
        // writes alongside it.
        var updated = await _db.Histories
            .Where(h => h.ExternalMessageId == messageId)
            .ExecuteUpdateAsync(set => set.SetProperty(h => h.Status, mapped), ct);

        return Ok(new { status = "success", updated });
    }
}
