namespace AILogic.Domain;

public sealed class Conversation
{
    private readonly List<ChatMessage> _messages = [];

    public Conversation(string id, Channel channel)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A conversation id is required.", nameof(id));
        }

        Id = id;
        Channel = channel;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Id { get; }
    public Channel Channel { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public ChatMessage AddMessage(ChatRole role, string content)
    {
        var cleanContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(cleanContent))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        var message = new ChatMessage(
            Guid.NewGuid(),
            Id,
            Channel,
            role,
            cleanContent,
            DateTimeOffset.UtcNow);

        _messages.Add(message);
        UpdatedAt = message.CreatedAt;
        return message;
    }
}
