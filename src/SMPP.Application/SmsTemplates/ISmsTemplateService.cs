using SMPP.Application.Common;

namespace SMPP.Application.SmsTemplates;

/// <summary>
/// SMS Templates (reusable, placeholder-driven message bodies), scoped strictly to their owner -
/// same ownership/visibility model as <c>ICampaignService</c>. Update/Delete verify ownership
/// before acting.
/// </summary>
public interface ISmsTemplateService
{
    Task<PagedResult<SmsTemplateListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default);

    Task<SmsTemplateDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default);

    Task<int> CreateAsync(int ownerUserId, CreateSmsTemplateRequest request, CancellationToken ct = default);

    Task UpdateAsync(int id, int ownerUserId, UpdateSmsTemplateRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default);
}
