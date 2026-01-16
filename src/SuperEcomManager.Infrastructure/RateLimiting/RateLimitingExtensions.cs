using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SuperEcomManager.Infrastructure.RateLimiting;

/// <summary>
/// Extension methods for configuring rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Rate limiting policy names.
    /// </summary>
    public static class PolicyNames
    {
        public const string Global = "global";
        public const string Api = "api";
        public const string Auth = "auth";
        public const string Bulk = "bulk";
        public const string Export = "export";
        public const string Webhook = "webhook";
        public const string Search = "search";
    }

    /// <summary>
    /// Adds rate limiting services to the service collection.
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitingSettings>(
            configuration.GetSection(RateLimitingSettings.SectionName));

        var settings = configuration
            .GetSection(RateLimitingSettings.SectionName)
            .Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        if (!settings.Enabled)
        {
            // Add a no-op rate limiter when disabled
            services.AddRateLimiter(_ => { });
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Configure the response for rejected requests
            options.OnRejected = async (context, cancellationToken) =>
            {
                var settingsOption = context.HttpContext.RequestServices
                    .GetService<IOptions<RateLimitingSettings>>();
                var message = settingsOption?.Value.RateLimitExceededMessage
                    ?? "Too many requests. Please try again later.";

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();

                var response = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = message,
                    retryAfter = (int)retryAfter
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

            // Global policy - applied to all requests
            options.AddPolicy(PolicyNames.Global, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Global.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Global.WindowSeconds),
                        SegmentsPerWindow = settings.Global.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Global.QueueLimit
                    }));

            // API policy - for standard authenticated API calls
            options.AddPolicy(PolicyNames.Api, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Api.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Api.WindowSeconds),
                        SegmentsPerWindow = settings.Api.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Api.QueueLimit
                    }));

            // Auth policy - stricter limits for authentication endpoints
            options.AddPolicy(PolicyNames.Auth, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Auth.WindowSeconds),
                        SegmentsPerWindow = settings.Auth.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Auth.QueueLimit
                    }));

            // Bulk policy - very limited for bulk operations
            options.AddPolicy(PolicyNames.Bulk, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Bulk.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Bulk.WindowSeconds),
                        SegmentsPerWindow = settings.Bulk.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Bulk.QueueLimit
                    }));

            // Export policy - limited exports to prevent abuse
            options.AddPolicy(PolicyNames.Export, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Export.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Export.WindowSeconds),
                        SegmentsPerWindow = settings.Export.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Export.QueueLimit
                    }));

            // Webhook policy - for webhook delivery endpoints
            options.AddPolicy(PolicyNames.Webhook, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Webhook.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Webhook.WindowSeconds),
                        SegmentsPerWindow = settings.Webhook.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Webhook.QueueLimit
                    }));

            // Search policy - moderate limits for search operations
            options.AddPolicy(PolicyNames.Search, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Search.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Search.WindowSeconds),
                        SegmentsPerWindow = settings.Search.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Search.QueueLimit
                    }));
        });

        return services;
    }

    /// <summary>
    /// Gets the partition key for rate limiting based on tenant and user.
    /// </summary>
    private static string GetPartitionKey(HttpContext context)
    {
        // Try to get tenant ID from header or claims
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? context.User.FindFirst("tenant_id")?.Value
            ?? "anonymous";

        // Try to get user ID from claims
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("user_id")?.Value
            ?? GetClientIp(context);

        return $"{tenantId}:{userId}";
    }

    /// <summary>
    /// Gets the client IP address for rate limiting.
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded headers (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check for real IP header (used by some proxies)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Adds rate limiting middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    /// <summary>
    /// Adds API usage tracking middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseApiUsageTracking(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiUsageMiddleware>();
    }
}
