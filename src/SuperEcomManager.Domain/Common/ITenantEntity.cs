namespace SuperEcomManager.Domain.Common;

/// <summary>
/// Interface for entities that belong to a specific tenant.
/// Used for multi-tenancy enforcement at the entity level.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant identifier this entity belongs to.
    /// This is automatically set based on the current tenant context.
    /// </summary>
    Guid TenantId { get; }
}
