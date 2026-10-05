using AILogic.Domain;

namespace AILogic.Application.Abstractions;

public interface IAgentClient
{
    Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
