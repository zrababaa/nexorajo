using SMPP.Application.Common;

namespace SMPP.Application.SpamKeywords;

public interface ISpamKeywordService
{
    Task<IReadOnlyList<SpamKeywordListItemDto>> GetAllAsync(CancellationToken ct = default);

    Task<int> CreateAsync(int createdByUserId, CreateSpamKeywordRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Flips a keyword between enabled and disabled without deleting it.</summary>
    Task SetEnabledAsync(int id, bool isEnabled, CancellationToken ct = default);

    /// <summary>Sends rejected by the content filter - see ISpamKeywordFilterService.LogBlockedAttemptAsync.</summary>
    Task<PagedResult<SpamBlockedAttemptRowDto>> GetBlockedAttemptsAsync(int page, int pageSize, CancellationToken ct = default);
}
