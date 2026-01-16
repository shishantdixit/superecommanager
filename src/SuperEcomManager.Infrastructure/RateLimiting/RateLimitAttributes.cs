namespace SuperEcomManager.Infrastructure.RateLimiting;

/// <summary>
/// Constants for rate limiting policy names.
/// Use these with [EnableRateLimiting(RateLimitPolicies.Api)] attribute.
/// </summary>
/// <example>
/// [EnableRateLimiting(RateLimitPolicies.Api)]
/// public class OrdersController : ControllerBase
/// {
///     [EnableRateLimiting(RateLimitPolicies.Bulk)]
///     public async Task<IActionResult> BulkUpdate() { }
///
///     [EnableRateLimiting(RateLimitPolicies.Export)]
///     public async Task<IActionResult> Export() { }
/// }
/// </example>
public static class RateLimitPolicies
{
    /// <summary>
    /// Global rate limit policy applied to all requests.
    /// 1000 requests per minute.
    /// </summary>
    public const string Global = RateLimitingExtensions.PolicyNames.Global;

    /// <summary>
    /// Standard API rate limit for authenticated requests.
    /// 300 requests per minute.
    /// </summary>
    public const string Api = RateLimitingExtensions.PolicyNames.Api;

    /// <summary>
    /// Strict rate limit for authentication endpoints.
    /// 10 requests per minute (prevents brute force).
    /// </summary>
    public const string Auth = RateLimitingExtensions.PolicyNames.Auth;

    /// <summary>
    /// Rate limit for bulk operations.
    /// 5 requests per minute.
    /// </summary>
    public const string Bulk = RateLimitingExtensions.PolicyNames.Bulk;

    /// <summary>
    /// Rate limit for export operations.
    /// 10 requests per 5 minutes.
    /// </summary>
    public const string Export = RateLimitingExtensions.PolicyNames.Export;

    /// <summary>
    /// Rate limit for webhook delivery endpoints.
    /// 100 requests per minute.
    /// </summary>
    public const string Webhook = RateLimitingExtensions.PolicyNames.Webhook;

    /// <summary>
    /// Rate limit for search operations.
    /// 60 requests per minute.
    /// </summary>
    public const string Search = RateLimitingExtensions.PolicyNames.Search;
}
