using System.Net.Http.Headers;
using System.Net.Http.Json;
using AILogic.Application.Abstractions;
using AILogic.Infrastructure.Configuration;

namespace AILogic.Infrastructure.Meta;

public sealed class WhatsAppClient(HttpClient httpClient, MetaOptions options) : IWhatsAppClient
{
    public async Task SendTextAsync(
        string recipientPhone,
        string text,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.GraphApiVersion}/{options.WhatsAppPhoneNumberId}/messages")
        {
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = recipientPhone,
                type = "text",
                text = new { preview_url = false, body = text }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            options.WhatsAppAccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.WhatsAppAccessToken) ||
            string.IsNullOrWhiteSpace(options.WhatsAppPhoneNumberId))
        {
            throw new InvalidOperationException(
                "WhatsApp is not configured. Set META_WHATSAPP_ACCESS_TOKEN and META_WHATSAPP_PHONE_NUMBER_ID.");
        }
    }
}
