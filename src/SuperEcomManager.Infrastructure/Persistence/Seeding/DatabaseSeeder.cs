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
        if (await dbContext.Permissions.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Permissions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding permissions...");

        var permissions = Permission.CreateAllPermissions().ToList();
        dbContext.Permissions.AddRange(permissions);

        _logger.LogInformation("Seeded {Count} permissions", permissions.Count);
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
