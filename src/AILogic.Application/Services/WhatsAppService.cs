using AILogic.Application.Abstractions;
using AILogic.Domain;

namespace AILogic.Application.Services;

public sealed class WhatsAppService(
    IChatService chatService,
    IWhatsAppClient whatsAppClient)
{
    public async Task HandleIncomingAsync(
        string senderPhone,
        string message,
        CancellationToken cancellationToken = default)
    {
        var reply = await chatService.ReplyAsync(
            Channel.WhatsApp,
            $"whatsapp:{senderPhone}",
            message,
            cancellationToken);

        await whatsAppClient.SendTextAsync(senderPhone, reply.Message, cancellationToken);
    }
}
