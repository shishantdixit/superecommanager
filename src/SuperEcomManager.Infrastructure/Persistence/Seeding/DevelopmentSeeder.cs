using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Platform;
using SuperEcomManager.Domain.Entities.Subscriptions;
using SuperEcomManager.Domain.Entities.Tenants;

namespace SuperEcomManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development seeder that creates a demo tenant for testing.
/// </summary>
public class DevelopmentSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DevelopmentSeeder> _logger;

    // Demo credentials
    public const string DemoTenantSlug = "demo";
    public const string DemoOwnerEmail = "admin@demo.com";
    public const string DemoOwnerPassword = "Admin@123";

    // Platform Admin credentials
    public const string PlatformAdminEmail = "superadmin@superecom.com";
    public const string PlatformAdminPassword = "SuperAdmin@123";

    public DevelopmentSeeder(
        IServiceProvider serviceProvider,
        ILogger<DevelopmentSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seeds development data including a demo tenant with owner user and platform admin.
    /// </summary>
    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantSeeder = scope.ServiceProvider.GetRequiredService<ITenantSeeder>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        _logger.LogInformation("Starting development data seeding...");

        // Seed platform admin first
        await SeedPlatformAdminAsync(dbContext, passwordHasher, cancellationToken);

        // Check if demo tenant already exists
        var demoTenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Slug == DemoTenantSlug, cancellationToken);

        if (demoTenant != null)
        {
            _logger.LogInformation("Demo tenant already exists, skipping development seeding");
            return;
        }

        // Get the free plan
        var freePlan = await dbContext.Plans
            .FirstOrDefaultAsync(p => p.Code == "free", cancellationToken);

        if (freePlan == null)
        {
            _logger.LogWarning("Free plan not found. Make sure shared data is seeded first.");
            return;
        }

        // Create demo tenant
        var tenant = Tenant.Create(
            name: "Demo Company",
            slug: DemoTenantSlug,
            contactEmail: DemoOwnerEmail,
            trialDays: 365); // Long trial for development

        tenant.UpdateProfile(
            companyName: "Demo Company Ltd",
            logoUrl: null,
            website: "https://demo.superecommanager.com",
            contactEmail: DemoOwnerEmail,
            contactPhone: "+91-9999999999",
            address: "123 Demo Street, Mumbai, India",
            gstNumber: "27AADCD1234A1Z5");

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Create subscription
        var subscription = Subscription.CreateTrial(tenant.Id, freePlan.Id, 365);
        dbContext.Subscriptions.Add(subscription);

        // Log activity
        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            Guid.Empty,
            TenantActivityActions.Created,
            "Demo tenant created for development");
        dbContext.TenantActivityLogs.Add(activityLog);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Initialize tenant schema with owner user
        try
        {
            await tenantSeeder.InitializeTenantAsync(
                tenant.Id,
                tenant.SchemaName,
                DemoOwnerEmail,
                DemoOwnerPassword,
                "Demo Company",
                cancellationToken);

            // Activate tenant
            tenant.Activate();
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("==============================================");
            _logger.LogInformation("Development seeding completed successfully!");
            _logger.LogInformation("Demo tenant created with credentials:");
            _logger.LogInformation("  Tenant Slug: {Slug}", DemoTenantSlug);
            _logger.LogInformation("  Email: {Email}", DemoOwnerEmail);
            _logger.LogInformation("  Password: {Password}", DemoOwnerPassword);
            _logger.LogInformation("----------------------------------------------");
            _logger.LogInformation("Platform Admin credentials:");
            _logger.LogInformation("  Email: {Email}", PlatformAdminEmail);
            _logger.LogInformation("  Password: {Password}", PlatformAdminPassword);
            _logger.LogInformation("==============================================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize demo tenant schema");
            throw;
        }
    }

    /// <summary>
    /// Seeds the default platform super admin.
    /// </summary>
    private async Task SeedPlatformAdminAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        // Check if platform admin already exists
        var existingAdmin = await dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Email == PlatformAdminEmail.ToLowerInvariant(), cancellationToken);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Platform admin already exists, skipping");
            return;
        }

        _logger.LogInformation("Creating platform super admin...");

        var hashedPassword = passwordHasher.HashPassword(PlatformAdminPassword);
        var platformAdmin = PlatformAdmin.Create(
            email: PlatformAdminEmail,
            firstName: "Super",
            lastName: "Admin",
            passwordHash: hashedPassword,
            isSuperAdmin: true);

        dbContext.PlatformAdmins.Add(platformAdmin);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform super admin created: {Email}", PlatformAdminEmail);
    }
}
