using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Entities.Platform;
using SuperEcomManager.Domain.Entities.Subscriptions;
using SuperEcomManager.Domain.Entities.Tenants;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Interface for the shared/public schema database context.
/// Contains entities that are shared across all tenants.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Plan> Plans { get; }
    DbSet<Feature> Features { get; }
    DbSet<PlanFeature> PlanFeatures { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<PlatformAdmin> PlatformAdmins { get; }
    DbSet<TenantActivityLog> TenantActivityLogs { get; }
    DbSet<PlatformSettings> PlatformSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
