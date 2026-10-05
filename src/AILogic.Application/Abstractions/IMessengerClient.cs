namespace AILogic.Application.Abstractions;

public interface IMessengerClient
{
    Task SendTextAsync(string recipientId, string text, CancellationToken cancellationToken = default);
}
