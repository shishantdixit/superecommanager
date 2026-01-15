namespace SuperEcomManager.Application.Common.Models;

/// <summary>
/// Standard API response wrapper.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public PaginationMeta? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Ok(T data, PaginationMeta pagination, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Pagination = pagination
        };
    }

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResponse<T> Fail(IEnumerable<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "One or more errors occurred.",
            Errors = errors
        };
    }
}

/// <summary>
/// Pagination metadata for API responses.
/// </summary>
public class PaginationMeta
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public static PaginationMeta FromPaginatedList<T>(PaginatedList<T> list)
    {
        return new PaginationMeta
        {
            CurrentPage = list.PageNumber,
            PageSize = list.PageSize,
            TotalPages = list.TotalPages,
            TotalCount = list.TotalCount,
            HasPrevious = list.HasPreviousPage,
            HasNext = list.HasNextPage
        };
    }
}
