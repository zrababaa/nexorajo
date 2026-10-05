namespace AILogic.Domain;

public sealed record ChatMessage(
    Guid Id,
    string ConversationId,
    Channel Channel,
    ChatRole Role,
    string Content,
    DateTimeOffset CreatedAt);
