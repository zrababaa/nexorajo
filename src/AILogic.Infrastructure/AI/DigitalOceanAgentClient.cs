using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AILogic.Application.Abstractions;
using AILogic.Application.Exceptions;
using AILogic.Domain;
using AILogic.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace AILogic.Infrastructure.AI;

public sealed class DigitalOceanAgentClient(
    HttpClient httpClient,
    DigitalOceanAgentOptions options,
    ILogger<DigitalOceanAgentClient> logger) : IAgentClient
{
    public async Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.AccessKey))
        {
            throw new AgentUnavailableException(
                "The AI agent is not configured. Set DIGITALOCEAN_AGENT_ENDPOINT and DIGITALOCEAN_AGENT_KEY.");
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new AgentUnavailableException("The configured AI agent endpoint is invalid.");
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/api/v1/chat/completions");

        var payload = new
        {
            messages = messages.Select(message => new
            {
                role = message.Role == ChatRole.User ? "user" : "assistant",
                content = message.Content
            }),
            stream = false,
            include_functions_info = false,
            include_retrieval_info = false,
            include_guardrails_info = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "DigitalOcean agent returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                throw new AgentUnavailableException(
                    $"The AI agent returned HTTP {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(responseBody);
            if (TryReadAnswer(document.RootElement, out var answer))
            {
                return answer;
            }

            logger.LogWarning("DigitalOcean agent response did not contain assistant text.");
            throw new AgentUnavailableException("The AI agent returned an empty response.");
        }
        catch (AgentUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AgentUnavailableException("The AI agent request timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Could not reach the DigitalOcean agent.");
            throw new AgentUnavailableException("The AI agent is currently unreachable.", exception);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "The DigitalOcean agent returned invalid JSON.");
            throw new AgentUnavailableException("The AI agent returned an invalid response.", exception);
        }
    }

    private static bool TryReadAnswer(JsonElement root, out string answer)
    {
        answer = string.Empty;

        if (!root.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return false;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return false;
        }

        answer = content.ValueKind switch
        {
            JsonValueKind.String => content.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Array => string.Join(
                "\n",
                content.EnumerateArray()
                    .Where(item => item.TryGetProperty("text", out _))
                    .Select(item => item.GetProperty("text").GetString())
                    .Where(text => !string.IsNullOrWhiteSpace(text))),
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(answer);
    }
}
