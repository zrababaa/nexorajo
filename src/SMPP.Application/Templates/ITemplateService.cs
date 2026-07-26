using SMPP.Application.Common;

namespace SMPP.Application.Templates;

/// <summary>
/// MessageSegmentCount is always computed server-side via ISegmentCounter - legacy trusted a
/// client-submitted "message_count" form field, which a user could tamper with to under-report
/// (and under-pay for) a bulk-via-template send.
/// </summary>
public interface ITemplateService
{
    Task<PagedResult<TemplateListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default);

    Task<TemplateDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default);

    Task<int> CreateAsync(int ownerUserId, CreateTemplateRequest request, CancellationToken ct = default);

    Task UpdateAsync(int id, int ownerUserId, UpdateTemplateRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default);
}
