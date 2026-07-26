namespace SMPP.Application.SpamKeywords;

public interface ISpamKeywordService
{
    Task<IReadOnlyList<SpamKeywordListItemDto>> GetAllAsync(CancellationToken ct = default);

    Task<int> CreateAsync(int createdByUserId, CreateSpamKeywordRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
