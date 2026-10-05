using System.ComponentModel.DataAnnotations;

namespace AILogic.API.Models;

public sealed class ChatRequest
{
    [StringLength(128)]
    public string? ConversationId { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}
