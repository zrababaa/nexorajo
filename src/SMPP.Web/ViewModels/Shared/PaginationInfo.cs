namespace SMPP.Web.ViewModels.Shared;

/// <summary>
/// One reusable pagination partial for every list page - legacy mixed server-side pagination
/// (Bootstrap pager) with fully client-side DataTables.net (no server paging at all) across
/// different screens with no consistent pattern.
/// </summary>
public record PaginationInfo(int PageNumber, int TotalPages);
