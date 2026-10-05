using System.Collections.Concurrent;
using AILogic.Application.Abstractions;
using AILogic.Domain;

namespace AILogic.Infrastructure.Persistence;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();

    public Conversation GetOrCreate(string conversationId, Channel channel)
    {
        var cleanId = conversationId?.Trim();
        if (string.IsNullOrWhiteSpace(cleanId))
        {
            throw new ArgumentException("A conversation id is required.", nameof(conversationId));
        }

        return _conversations.GetOrAdd(
            $"{channel}:{cleanId}",
            _ => new Conversation(cleanId, channel));
    }
}
