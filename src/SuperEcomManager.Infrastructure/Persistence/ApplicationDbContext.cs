using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Entities.Subscriptions;
using SuperEcomManager.Domain.Entities.Tenants;

namespace SuperEcomManager.Infrastructure.Persistence;

/// <summary>
/// Database context for shared/application-level data.
/// Contains entities that are not tenant-specific.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for shared tables
        modelBuilder.HasDefaultSchema("shared");

        // Ignore DomainEvent - it's not a database entity
        modelBuilder.Ignore<Domain.Common.DomainEvent>();

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly,
            type => type.Namespace?.Contains("Configurations.Shared") ?? false);
    }
}
