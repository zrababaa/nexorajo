using SMPP.Domain.Enums;

namespace SMPP.Application.SpamKeywords;

public record SpamKeywordListItemDto(int Id, string Keyword, SpamKeywordType KeywordType, DateTime CreatedAt);

public record CreateSpamKeywordRequest(string Keyword, SpamKeywordType KeywordType);
