using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Database seeder for initial setup and development data.
/// </summary>
public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IServiceProvider serviceProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all required data for the shared schema.
    /// </summary>
    public async Task SeedSharedDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Starting shared data seeding...");

        await SeedFeaturesAsync(dbContext, cancellationToken);
        await SeedPermissionsAsync(dbContext, cancellationToken);
        await SeedPlansAsync(dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shared data seeding completed");
    }

    private async Task SeedFeaturesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Features.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Features already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding features...");

        var features = Feature.CreateAllFeatures().ToList();
        dbContext.Features.AddRange(features);

        _logger.LogInformation("Seeded {Count} features", features.Count);
    }

    private async Task SeedPermissionsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet();

        var allPermissions = Permission.CreateAllPermissions().ToList();
        var newPermissions = allPermissions.Where(p => !existingCodes.Contains(p.Code)).ToList();

        if (newPermissions.Count == 0)
        {
            _logger.LogDebug("All permissions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding {Count} new permissions...", newPermissions.Count);

        dbContext.Permissions.AddRange(newPermissions);

        _logger.LogInformation("Seeded {Count} new permissions (total: {Total})", newPermissions.Count, existingPermissions.Count + newPermissions.Count);
    }

    /// <summary>
    /// Syncs permissions from shared schema to all tenant schemas and updates Owner roles.
    /// </summary>
    public async Task SyncPermissionsToTenantsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currentTenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();

        // Get all active tenants
        var allTenants = await appDbContext.Tenants.ToListAsync(cancellationToken);
        var tenants = allTenants.Where(t => t.IsActive()).ToList();

        var sharedPermissions = await appDbContext.Permissions.ToListAsync(cancellationToken);
        _logger.LogInformation("Syncing {Count} permissions to {TenantCount} tenants", sharedPermissions.Count, tenants.Count);

        foreach (var tenant in tenants)
        {
            try
            {
                // Set tenant context
                currentTenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

                // Get tenant-scoped DbContext
                using var tenantScope = _serviceProvider.CreateScope();
                var tenantCurrentService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
                tenantCurrentService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

                var tenantDbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();

                // Get existing permissions
                var existingPermissions = await tenantDbContext.Permissions.ToListAsync(cancellationToken);
                var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet();

                var newPermissions = sharedPermissions.Where(p => !existingCodes.Contains(p.Code)).ToList();

                if (newPermissions.Count == 0)
                {
                    _logger.LogDebug("Tenant {TenantSlug}: No new permissions to sync", tenant.Slug);
                    continue;
                }

                // Add new permissions
                foreach (var perm in newPermissions)
                {
                    var tenantPerm = Permission.Create(perm.Code, perm.Name, perm.Module, perm.Description);
                    tenantDbContext.Permissions.Add(tenantPerm);
                }
                await tenantDbContext.SaveChangesAsync(cancellationToken);

                // Add to Owner role
                var ownerRole = await tenantDbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Owner", cancellationToken);
                if (ownerRole != null)
                {
                    var tenantNewPerms = await tenantDbContext.Permissions
                        .Where(p => newPermissions.Select(np => np.Code).Contains(p.Code))
                        .ToListAsync(cancellationToken);

                    // Check which permissions Owner already has
                    var existingRolePermIds = await tenantDbContext.RolePermissions
                        .Where(rp => rp.RoleId == ownerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync(cancellationToken);

                    foreach (var perm in tenantNewPerms.Where(p => !existingRolePermIds.Contains(p.Id)))
                    {
                        tenantDbContext.RolePermissions.Add(new RolePermission(ownerRole.Id, perm.Id));
                    }
                    await tenantDbContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Tenant {TenantSlug}: Synced {Count} new permissions", tenant.Slug, newPermissions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync permissions to tenant {TenantSlug}", tenant.Slug);
            }
        }

        _logger.LogInformation("Permission sync completed for all tenants");
    }

    private async Task SeedPlansAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Plans.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Plans already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding plans...");

        var features = await dbContext.Features.ToListAsync(cancellationToken);
        var featuresByCode = features.ToDictionary(f => f.Code);

        // Free plan
        var freePlan = Plan.Create(
            name: "Free",
            code: "free",
            monthlyPrice: 0,
            yearlyPrice: 0,
            maxUsers: 2,
            maxOrders: 100,
            maxChannels: 1);
        freePlan.SetDescription("Perfect for getting started");
        freePlan.SetSortOrder(1);
        dbContext.Plans.Add(freePlan);

        // Basic plan
        var basicPlan = Plan.Create(
            name: "Basic",
            code: "basic",
            monthlyPrice: 999,
            yearlyPrice: 9990,
            maxUsers: 5,
            maxOrders: 1000,
            maxChannels: 2);
        basicPlan.SetDescription("For small businesses");
        basicPlan.SetSortOrder(2);
        dbContext.Plans.Add(basicPlan);

        // Professional plan
        var proPlan = Plan.Create(
            name: "Professional",
            code: "professional",
            monthlyPrice: 2499,
            yearlyPrice: 24990,
            maxUsers: 15,
            maxOrders: 5000,
            maxChannels: 5);
        proPlan.SetDescription("For growing businesses");
        proPlan.SetSortOrder(3);
        dbContext.Plans.Add(proPlan);

        // Enterprise plan
        var enterprisePlan = Plan.Create(
            name: "Enterprise",
            code: "enterprise",
            monthlyPrice: 9999,
            yearlyPrice: 99990,
            maxUsers: -1, // unlimited
            maxOrders: -1, // unlimited
            maxChannels: -1); // unlimited
        enterprisePlan.SetDescription("For large enterprises");
        enterprisePlan.SetSortOrder(4);
        dbContext.Plans.Add(enterprisePlan);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed plan features
        await SeedPlanFeaturesAsync(dbContext, freePlan, basicPlan, proPlan, enterprisePlan, featuresByCode, cancellationToken);

        _logger.LogInformation("Seeded 4 plans with features");
    }

    private async Task SeedPlanFeaturesAsync(
        ApplicationDbContext dbContext,
        Plan freePlan,
        Plan basicPlan,
        Plan proPlan,
        Plan enterprisePlan,
        Dictionary<string, Feature> features,
        CancellationToken cancellationToken)
    {
        var planFeatures = new List<PlanFeature>();

        // Free plan features
        var freeFeatures = new[] { "orders_management", "shipments_management", "analytics_basic" };
        foreach (var featureCode in freeFeatures)
        {
            if (features.TryGetValue(featureCode, out var feature))
            {
                planFeatures.Add(new PlanFeature(freePlan.Id, feature.Id));
            }
        }

        // Basic plan features (includes free + more)
        var basicFeatures = new[]
        {
            "orders_management", "shipments_management", "analytics_basic",
            "ndr_management", "inventory_management", "team_management"
        };
        foreach (var featureCode in basicFeatures)
        {
            if (features.TryGetValue(featureCode, out var feature))
            {
                planFeatures.Add(new PlanFeature(basicPlan.Id, feature.Id));
            }
        }

        // Professional plan features (includes basic + more)
        var proFeatures = new[]
        {
            "orders_management", "shipments_management", "analytics_basic",
            "ndr_management", "inventory_management", "team_management",
            "analytics_advanced", "multi_channel", "finance_management",
            "bulk_operations", "api_access", "webhooks"
        };
        foreach (var featureCode in proFeatures)
        {
            if (features.TryGetValue(featureCode, out var feature))
            {
                planFeatures.Add(new PlanFeature(proPlan.Id, feature.Id));
            }
        }

        // Enterprise plan features (all features)
        foreach (var feature in features.Values)
        {
            planFeatures.Add(new PlanFeature(enterprisePlan.Id, feature.Id));
        }

        dbContext.PlanFeatures.AddRange(planFeatures);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Extension methods for Plan entity used in seeding.
/// </summary>
file static class PlanExtensions
{
    public static void SetDescription(this Plan plan, string description)
    {
        // Using reflection to set private property for seeding
        var property = typeof(Plan).GetProperty(nameof(Plan.Description));
        property?.SetValue(plan, description);
    }

    public static void SetSortOrder(this Plan plan, int sortOrder)
    {
        var property = typeof(Plan).GetProperty(nameof(Plan.SortOrder));
        property?.SetValue(plan, sortOrder);
    }
}
