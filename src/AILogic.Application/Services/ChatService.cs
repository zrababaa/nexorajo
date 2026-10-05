using AILogic.Application.Abstractions;
using AILogic.Application.Models;
using AILogic.Domain;

namespace AILogic.Application.Services;

public sealed class ChatService(
    IConversationStore conversationStore,
    IAgentClient agentClient) : IChatService
{
    private const int MaxHistoryMessages = 20;

    public async Task<ChatReply> ReplyAsync(
        Channel channel,
        string conversationId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var conversation = conversationStore.GetOrCreate(conversationId, channel);
        IReadOnlyList<ChatMessage> history;

        lock (conversation)
        {
            conversation.AddMessage(ChatRole.User, message);
            history = conversation.Messages.TakeLast(MaxHistoryMessages).ToArray();
        }

        var answer = await agentClient.CompleteAsync(history, cancellationToken);

        lock (conversation)
        {
            conversation.AddMessage(ChatRole.Assistant, answer);
        }

        return new ChatReply(conversation.Id, answer);
    }
}
