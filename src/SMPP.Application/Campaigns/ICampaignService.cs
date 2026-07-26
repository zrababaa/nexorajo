using SMPP.Application.Common;

namespace SMPP.Application.Campaigns;

/// <summary>
/// Campaigns (recipient lists) are scoped strictly to their owner in every role, mirroring
/// legacy behavior - unlike Transactions/History there is no cross-user visibility here.
/// Update/Delete verify ownership before acting: legacy's equivalent actions took no such
/// check (any authenticated user who could produce/guess an ID could edit or delete another
/// user's campaign) - this closes that access-control gap.
/// </summary>
public interface ICampaignService
{
    Task<PagedResult<CampaignListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default);

    Task<CampaignDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default);

    Task<int> CreateAsync(int ownerUserId, CreateCampaignRequest request, CancellationToken ct = default);

    Task UpdateAsync(int id, int ownerUserId, UpdateCampaignRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default);
}
