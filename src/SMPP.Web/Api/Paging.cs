namespace SMPP.Web.Api;

/// <summary>
/// Clamps the paging parameters every list endpoint takes. A caller is free to send anything;
/// this keeps page numbers positive and stops a single request from asking for the whole table.
/// </summary>
public static class Paging
{
    public const int MaxPageSize = 200;
    public const int DefaultPageSize = 20;

    public static int Page(int page) => page < 1 ? 1 : page;

    public static int Size(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => pageSize,
    };
}
