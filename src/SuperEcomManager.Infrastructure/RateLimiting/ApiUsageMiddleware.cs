using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SuperEcomManager.Infrastructure.RateLimiting;

/// <summary>
/// Middleware that tracks API usage per tenant.
/// </summary>
public class ApiUsageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiUsageMiddleware> _logger;

    public ApiUsageMiddleware(RequestDelegate next, ILogger<ApiUsageMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiUsageTracker usageTracker)
    {
        // Only track API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var tenantId = GetTenantId(context);
        var endpoint = GetEndpointName(context);

        try
        {
            await _next(context);
        }
        finally
        {
            // Track the request after it completes
            if (!string.IsNullOrEmpty(tenantId))
            {
                await usageTracker.RecordRequestAsync(tenantId, endpoint, context.RequestAborted);
            }
        }
    }

    private static string GetTenantId(HttpContext context)
    {
        // Try to get tenant ID from header
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        // Try from claims if not in header
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.User.FindFirst("tenant_id")?.Value;
        }

        return tenantId ?? "anonymous";
    }

    private static string GetEndpointName(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // Normalize the path by removing GUIDs and IDs
        var normalizedPath = NormalizePath(path);

        return $"{method} {normalizedPath}";
    }

    private static string NormalizePath(string path)
    {
        // Replace GUIDs with {id} placeholder
        var guidPattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
        path = System.Text.RegularExpressions.Regex.Replace(path, guidPattern, "{id}");

        // Replace numeric IDs with {id} placeholder
        path = System.Text.RegularExpressions.Regex.Replace(path, @"/\d+(?=/|$)", "/{id}");

        return path;
    }
}
