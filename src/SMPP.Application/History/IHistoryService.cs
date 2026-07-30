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

    /// <summary>Full-filter version backing the dedicated History tab.</summary>
    Task<PagedResult<HistoryListItemDto>> GetPagedAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, int page, int pageSize, CancellationToken ct = default);

    Task<HistorySummaryDto> GetSummaryAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, CancellationToken ct = default);

    /// <summary>Unbounded (capped) row set for CSV/Excel export, honoring the same filters as the list view.</summary>
    Task<IReadOnlyList<HistoryExportRowDto>> GetForExportAsync(
        int currentUserId, UserRole role, HistoryFilterDto filter, CancellationToken ct = default);
}
