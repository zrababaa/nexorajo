using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.LinkTracking;

/// <summary>
/// The read path behind the click-stats screen, split from ILinkTrackingService the same way
/// the spam filter's write path (ISpamKeywordFilterService) is split from its admin CRUD
/// service - the reporting reads need IUserScopeResolver-scoped visibility that the send-time
/// writer has no reason to depend on.
///
/// Both methods return null when the batch has no tracked links or is not visible to the
/// caller, so the controller can map either case to the same 404 - the same pattern
/// CampaignsApiController.GetById already uses.
/// </summary>
public interface ILinkClickReportService
{
    /// <summary>
    /// Every tracked link visible to the caller, newest first, for the standalone Tracking Links
    /// page. Scoped through IUserScopeResolver the same way GetBatchStatsAsync is: an Account sees
    /// only its own links, a Superadmin sees all of them (with OwnerUsername filled in).
    /// </summary>
    Task<PagedResult<TrackedLinkRowDto>> GetLinksAsync(
        int currentUserId, UserRole role, int page, int pageSize, CancellationToken ct = default);

    Task<BatchLinkStatsDto?> GetBatchStatsAsync(string batchId, int currentUserId, UserRole role, CancellationToken ct = default);

    Task<PagedResult<LinkClickRowDto>?> GetBatchClicksAsync(
        string batchId, int currentUserId, UserRole role, int page, int pageSize, CancellationToken ct = default);
}
