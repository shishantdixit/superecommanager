using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentTenantService.
/// Stores tenant context per-request using AsyncLocal.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    private static readonly AsyncLocal<TenantInfo?> _currentTenant = new();

    public Guid TenantId => _currentTenant.Value?.TenantId ?? Guid.Empty;
    public string SchemaName => _currentTenant.Value?.SchemaName ?? "public";
    public string TenantSlug => _currentTenant.Value?.TenantSlug ?? string.Empty;
    public bool HasTenant => _currentTenant.Value != null;

    public void SetTenant(Guid tenantId, string schemaName, string tenantSlug)
    {
        _currentTenant.Value = new TenantInfo(tenantId, schemaName, tenantSlug);
    }

    private sealed record TenantInfo(Guid TenantId, string SchemaName, string TenantSlug);
}
