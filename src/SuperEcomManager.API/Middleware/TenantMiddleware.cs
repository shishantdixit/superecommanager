using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.API.Middleware;

/// <summary>
/// Middleware for resolving and setting tenant context.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    // Paths that don't require tenant context
    private static readonly string[] TenantExemptPaths = new[]
    {
        "/health",
        "/swagger",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/forgot-password"
    };

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        // Skip tenant resolution for exempt paths
        if (TenantExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Try to resolve tenant from various sources
        var tenantIdentifier = ResolveTenantIdentifier(context);

        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            // No tenant identifier - proceed without tenant context
            // Authorization behaviors will handle tenant-required requests
            await _next(context);
            return;
        }

        // Look up tenant in database
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Slug == tenantIdentifier ||
                t.Id.ToString() == tenantIdentifier);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found: {TenantIdentifier}", tenantIdentifier);
            await _next(context);
            return;
        }

        if (tenant.DeletedAt.HasValue)
        {
            _logger.LogWarning("Tenant is deleted: {TenantId}", tenant.Id);
            await _next(context);
            return;
        }

        // Set tenant context
        tenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

        _logger.LogDebug("Tenant context set: {TenantId} ({TenantSlug})", tenant.Id, tenant.Slug);

        await _next(context);
    }

    private static string? ResolveTenantIdentifier(HttpContext context)
    {
        // Try header first (X-Tenant-Id or X-Tenant-Slug)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            return tenantIdHeader.FirstOrDefault();
        }

        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var tenantSlugHeader))
        {
            return tenantSlugHeader.FirstOrDefault();
        }

        // Try from JWT claims
        var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim))
        {
            return tenantClaim;
        }

        // Try from subdomain (e.g., tenant1.superecom.com)
        var host = context.Request.Host.Host;
        if (host.Contains('.'))
        {
            var subdomain = host.Split('.')[0];
            if (subdomain != "www" && subdomain != "api")
            {
                return subdomain;
            }
        }

        // Try from route parameter
        if (context.Request.RouteValues.TryGetValue("tenantSlug", out var routeTenant))
        {
            return routeTenant?.ToString();
        }

        return null;
    }
}
