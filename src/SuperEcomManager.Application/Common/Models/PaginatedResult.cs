namespace SuperEcomManager.Application.Common.Models;

/// <summary>
/// Represents a paginated result for API responses.
/// </summary>
public record PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedResult(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    /// <summary>
    /// Creates an empty paginated result.
    /// </summary>
    public static PaginatedResult<T> Empty(int page = 1, int pageSize = 20)
    {
        return new PaginatedResult<T>(Array.Empty<T>(), 0, page, pageSize);
    }
}
