using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

public class SpamKeyword : AuditableEntity, IHasCreator
{
    public string Keyword { get; set; } = string.Empty;
    public SpamKeywordType KeywordType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int CreatedByUserId { get; set; }
}
