namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current tenant information.
/// </summary>
public interface ICurrentTenantService
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the current tenant's database schema name.
    /// </summary>
    string SchemaName { get; }

    /// <summary>
    /// Gets the current tenant's slug.
    /// </summary>
    string TenantSlug { get; }

    /// <summary>
    /// Indicates whether a tenant context is available.
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// Sets the current tenant context.
    /// </summary>
    void SetTenant(Guid tenantId, string schemaName, string tenantSlug);
}
