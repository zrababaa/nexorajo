namespace AILogic.Application.Abstractions;

public interface IWhatsAppClient
{
    Task SendTextAsync(string recipientPhone, string text, CancellationToken cancellationToken = default);
}
