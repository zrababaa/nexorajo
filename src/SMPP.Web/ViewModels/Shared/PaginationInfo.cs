using SMPP.Application.Common;

namespace SMPP.Web.ViewModels.Shared;

/// <summary>
/// One reusable pagination partial for every list page - legacy mixed server-side pagination
/// (Bootstrap pager) with fully client-side DataTables.net (no server paging at all) across
/// different screens with no consistent pattern.
/// </summary>
public record PaginationInfo(int PageNumber, int TotalPages, int TotalCount, int PageSize, string QueryKey = "page")
{
    /// <summary>
    /// <paramref name="queryKey"/> only needs to change from the "page" default when a single
    /// view hosts more than one independently-paged table (see SpamKeywords/Index.cshtml) - each
    /// pager then needs its own query string key so paging one table doesn't reset the other.
    /// </summary>
    public static PaginationInfo From<T>(PagedResult<T> result, string queryKey = "page") =>
        new(result.PageNumber, result.TotalPages, result.TotalCount, result.PageSize, queryKey);
}
