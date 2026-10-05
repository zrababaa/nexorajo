using System.Security.Cryptography;
using System.Text.Json;
using AILogic.Application.Services;
using AILogic.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace AILogic.API.Controllers;

[ApiController]
[Route("api/webhooks/messenger")]
public sealed class MessengerWebhookController(
    MessengerService messengerService,
    MetaOptions metaOptions) : ControllerBase
{
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" &&
            !string.IsNullOrEmpty(metaOptions.WebhookVerifyToken) &&
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(verifyToken ?? string.Empty),
                System.Text.Encoding.UTF8.GetBytes(metaOptions.WebhookVerifyToken)))
        {
            return Content(challenge ?? string.Empty, "text/plain");
        }

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("entry", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            return Ok();
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("messaging", out var events) ||
                events.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var messagingEvent in events.EnumerateArray())
            {
                if (!TryReadMessage(messagingEvent, out var senderId, out var text))
                {
                    continue;
                }

                await messengerService.HandleIncomingAsync(senderId, text, cancellationToken);
            }
        }

        return Ok();
    }

    private static bool TryReadMessage(
        JsonElement messagingEvent,
        out string senderId,
        out string text)
    {
        senderId = string.Empty;
        text = string.Empty;

        if (!messagingEvent.TryGetProperty("sender", out var sender) ||
            !sender.TryGetProperty("id", out var senderValue) ||
            !messagingEvent.TryGetProperty("message", out var message) ||
            (message.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean()) ||
            !message.TryGetProperty("text", out var textValue))
        {
            return false;
        }

        senderId = senderValue.GetString() ?? string.Empty;
        text = textValue.GetString() ?? string.Empty;
        return senderId.Length > 0 && text.Length > 0;
    }
}
