using SMPP.Domain.Enums;

namespace SMPP.Application.SpamKeywords;

public record SpamKeywordListItemDto(int Id, string Keyword, SpamKeywordType KeywordType, bool IsEnabled, DateTime CreatedAt);

public record CreateSpamKeywordRequest(string Keyword, SpamKeywordType KeywordType);
