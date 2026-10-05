using System.Net.Http.Headers;
using System.Net.Http.Json;
using AILogic.Application.Abstractions;
using AILogic.Infrastructure.Configuration;

namespace AILogic.Infrastructure.Meta;

public sealed class MessengerClient(HttpClient httpClient, MetaOptions options) : IMessengerClient
{
    public async Task SendTextAsync(
        string recipientId,
        string text,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.GraphApiVersion}/me/messages")
        {
            Content = JsonContent.Create(new
            {
                recipient = new { id = recipientId },
                messaging_type = "RESPONSE",
                message = new { text }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            options.MessengerPageAccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.MessengerPageAccessToken))
        {
            throw new InvalidOperationException(
                "Messenger is not configured. Set META_MESSENGER_PAGE_ACCESS_TOKEN.");
        }
    }
}
