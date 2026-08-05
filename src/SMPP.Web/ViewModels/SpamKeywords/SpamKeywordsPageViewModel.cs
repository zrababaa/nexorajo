using SMPP.Application.Common;
using SMPP.Application.SpamKeywords;

namespace SMPP.Web.ViewModels.SpamKeywords;

public class SpamKeywordsPageViewModel
{
    public PagedResult<SpamKeywordListItemDto> Keywords { get; set; } = new();
    public PagedResult<SpamBlockedAttemptRowDto> BlockedAttempts { get; set; } = new();
}
