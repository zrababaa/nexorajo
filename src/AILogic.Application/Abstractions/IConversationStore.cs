using AILogic.Domain;

namespace AILogic.Application.Abstractions;

public interface IConversationStore
{
    Conversation GetOrCreate(string conversationId, Channel channel);
}
