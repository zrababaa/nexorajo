using SMPP.Application.Common;
using SMPP.Application.SpamKeywords;

namespace SMPP.Web.ViewModels.SpamKeywords;

public class SpamKeywordsPageViewModel
{
    public IReadOnlyList<SpamKeywordListItemDto> Keywords { get; set; } = Array.Empty<SpamKeywordListItemDto>();
    public PagedResult<SpamBlockedAttemptRowDto> BlockedAttempts { get; set; } = new();
}
