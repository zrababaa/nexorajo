using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AILogic.Application.Services;
using AILogic.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace AILogic.API.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
public sealed class WhatsAppWebhookController(
    WhatsAppService whatsAppService,
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
                Encoding.UTF8.GetBytes(verifyToken ?? string.Empty),
                Encoding.UTF8.GetBytes(metaOptions.WebhookVerifyToken)))
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
            if (!entry.TryGetProperty("changes", out var changes) ||
                changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value) ||
                    !value.TryGetProperty("messages", out var messages) ||
                    messages.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var message in messages.EnumerateArray())
                {
                    if (!TryReadMessage(message, out var senderPhone, out var text))
                    {
                        continue;
                    }

                    await whatsAppService.HandleIncomingAsync(senderPhone, text, cancellationToken);
                }
            }
        }

        return Ok();
    }

    private static bool TryReadMessage(
        JsonElement message,
        out string senderPhone,
        out string text)
    {
        senderPhone = string.Empty;
        text = string.Empty;

        if (!message.TryGetProperty("from", out var from) ||
            !message.TryGetProperty("type", out var type) ||
            type.GetString() != "text" ||
            !message.TryGetProperty("text", out var textObject) ||
            !textObject.TryGetProperty("body", out var body))
        {
            return false;
        }

        senderPhone = from.GetString() ?? string.Empty;
        text = body.GetString() ?? string.Empty;
        return senderPhone.Length > 0 && text.Length > 0;
    }
}
