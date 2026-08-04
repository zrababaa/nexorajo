using SMPP.Domain.Enums;

namespace SMPP.Application.SpamKeywords;

public record SpamKeywordListItemDto(int Id, string Keyword, SpamKeywordType KeywordType, bool IsEnabled, DateTime CreatedAt);

public record CreateSpamKeywordRequest(string Keyword, SpamKeywordType KeywordType);

public record SpamBlockedAttemptRowDto(
    int Id,
    DateTime CreatedAt,
    string Username,
    MessageSource Source,
    string SenderId,
    int RecipientCount,
    string MatchedTerms,
    string Message);
