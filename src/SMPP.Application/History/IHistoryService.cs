using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.History;

/// <summary>
/// One unified history query for Quick Send, Bulk Send, and the public API - scoped via
/// IUserScopeResolver (Account sees only its own rows, Superadmin sees everyone's).
/// </summary>
public interface IHistoryService
{
    Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, MessageSource? source, string? campaignBatchId, int page, int pageSize, CancellationToken ct = default);

    Task<HistorySummaryDto> GetSummaryAsync(int currentUserId, UserRole role, string campaignBatchId, CancellationToken ct = default);
}
