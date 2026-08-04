using SMPP.Domain.Common;
using SMPP.Domain.Enums;

namespace SMPP.Domain.Entities;

/// <summary>
/// One row per send rejected by the content filter (see SpamKeywordFilterService). Nothing is
/// charged or queued when this happens - the row exists purely so a Superadmin can see who tried
/// to send what, and which keyword(s) caught it.
/// </summary>
public class SpamBlockedAttempt : AuditableEntity
{
    public int UserId { get; set; }
    public MessageSource Source { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MatchedTerms { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
}
