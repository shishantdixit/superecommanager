namespace SuperEcomManager.Infrastructure.RateLimiting;

/// <summary>
/// Configuration settings for rate limiting policies.
/// </summary>
public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Whether rate limiting is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Global rate limit settings (applied to all endpoints).
    /// </summary>
    public RateLimitPolicy Global { get; set; } = new()
    {
        PermitLimit = 1000,
        WindowSeconds = 60,
        QueueLimit = 10
    };

    /// <summary>
    /// Rate limit for authenticated API requests.
    /// </summary>
    public RateLimitPolicy Api { get; set; } = new()
    {
        PermitLimit = 300,
        WindowSeconds = 60,
        QueueLimit = 5
    };

    /// <summary>
    /// Rate limit for authentication endpoints (login, register, etc.).
    /// </summary>
    public RateLimitPolicy Auth { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 2
    };

    /// <summary>
    /// Rate limit for bulk operations.
    /// </summary>
    public RateLimitPolicy Bulk { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        QueueLimit = 1
    };

    /// <summary>
    /// Rate limit for export operations.
    /// </summary>
    public RateLimitPolicy Export { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 300,
        QueueLimit = 2
    };

    /// <summary>
    /// Rate limit for webhook deliveries (per tenant).
    /// </summary>
    public RateLimitPolicy Webhook { get; set; } = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60,
        QueueLimit = 20
    };

    /// <summary>
    /// Rate limit for search operations.
    /// </summary>
    public RateLimitPolicy Search { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 5
    };

    /// <summary>
    /// Whether to include rate limit headers in responses.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Custom message for rate limit exceeded responses.
    /// </summary>
    public string RateLimitExceededMessage { get; set; } = "Too many requests. Please try again later.";
}

/// <summary>
/// Configuration for a single rate limit policy.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Maximum number of requests permitted within the window.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// The time window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// Maximum number of requests to queue when limit is reached.
    /// </summary>
    public int QueueLimit { get; set; }

    /// <summary>
    /// Segments per window for sliding window algorithm.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 4;
}
