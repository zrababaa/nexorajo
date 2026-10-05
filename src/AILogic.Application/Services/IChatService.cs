using AILogic.Application.Models;
using AILogic.Domain;

namespace AILogic.Application.Services;

public interface IChatService
{
    Task<ChatReply> ReplyAsync(
        Channel channel,
        string conversationId,
        string message,
        CancellationToken cancellationToken = default);
}
