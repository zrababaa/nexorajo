using AILogic.Application.Abstractions;
using AILogic.Domain;

namespace AILogic.Application.Services;

public sealed class MessengerService(
    IChatService chatService,
    IMessengerClient messengerClient)
{
    public async Task HandleIncomingAsync(
        string senderId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var reply = await chatService.ReplyAsync(
            Channel.Messenger,
            $"messenger:{senderId}",
            message,
            cancellationToken);

        await messengerClient.SendTextAsync(senderId, reply.Message, cancellationToken);
    }
}
