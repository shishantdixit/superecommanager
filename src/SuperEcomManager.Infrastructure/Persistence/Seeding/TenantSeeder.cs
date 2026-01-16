using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Entities.Settings;
using SuperEcomManager.Infrastructure.Authentication;

namespace SuperEcomManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeder for initializing tenant-specific data.
/// </summary>
public class TenantSeeder : ITenantSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(
        IServiceProvider serviceProvider,
        ILogger<TenantSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new tenant schema with required data.
    /// </summary>
    public async Task InitializeTenantAsync(
        Guid tenantId,
        string schemaName,
        string ownerEmail,
        string ownerPassword,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing tenant {TenantId} with schema {Schema}", tenantId, schemaName);

        using var scope = _serviceProvider.CreateScope();

        // Create schema
        await CreateTenantSchemaAsync(scope, schemaName, cancellationToken);

        // Seed default roles
        await SeedDefaultRolesAsync(scope, schemaName, cancellationToken);

        // Create owner user
        await CreateOwnerUserAsync(scope, schemaName, ownerEmail, ownerPassword, cancellationToken);

        // Create default settings
        await CreateDefaultSettingsAsync(scope, schemaName, companyName, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} initialization completed", tenantId);
    }

    private async Task CreateTenantSchemaAsync(IServiceScope scope, string schemaName, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        // Create schema if it doesn't exist
        await dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"",
            cancellationToken);

        // Apply migrations to the tenant schema
        await dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogDebug("Created schema {Schema}", schemaName);
    }

    private async Task SeedDefaultRolesAsync(IServiceScope scope, string schemaName, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await dbContext.Roles.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Roles already exist for tenant, skipping");
            return;
        }

        var permissions = await appDbContext.Permissions.ToListAsync(cancellationToken);
        var permissionsByCode = permissions.ToDictionary(p => p.Code);

        // Owner role (all permissions)
        var ownerRole = Role.Create("Owner", "Full access to all features", true);
        dbContext.Roles.Add(ownerRole);

        // Admin role (most permissions except critical security)
        var adminRole = Role.Create("Admin", "Administrative access", false);
        dbContext.Roles.Add(adminRole);

        // Manager role (operations management)
        var managerRole = Role.Create("Manager", "Operations management", false);
        dbContext.Roles.Add(managerRole);

        // Staff role (basic operations)
        var staffRole = Role.Create("Staff", "Basic operational access", false);
        dbContext.Roles.Add(staffRole);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Assign permissions to roles
        var rolePermissions = new List<RolePermission>();

        // Owner gets all permissions
        foreach (var permission in permissions)
        {
            rolePermissions.Add(new RolePermission(ownerRole.Id, permission.Id));
        }

        // Admin permissions (exclude sensitive security)
        var adminExclude = new HashSet<string> { "security.force_logout", "security.export_approve" };
        foreach (var permission in permissions.Where(p => !adminExclude.Contains(p.Code)))
        {
            rolePermissions.Add(new RolePermission(adminRole.Id, permission.Id));
        }

        // Manager permissions
        var managerPermissions = new[]
        {
            "orders.view", "orders.create", "orders.edit", "orders.cancel", "orders.export",
            "shipments.view", "shipments.create", "shipments.cancel", "shipments.track", "shipments.export",
            "ndr.view", "ndr.action", "ndr.reattempt",
            "inventory.view", "inventory.adjust",
            "team.view",
            "analytics.view",
            "data.view_masked"
        };
        foreach (var permCode in managerPermissions)
        {
            if (permissionsByCode.TryGetValue(permCode, out var permission))
            {
                rolePermissions.Add(new RolePermission(managerRole.Id, permission.Id));
            }
        }

        // Staff permissions
        var staffPermissions = new[]
        {
            "orders.view", "orders.create",
            "shipments.view", "shipments.track",
            "ndr.view", "ndr.action",
            "inventory.view",
            "data.view_masked"
        };
        foreach (var permCode in staffPermissions)
        {
            if (permissionsByCode.TryGetValue(permCode, out var permission))
            {
                rolePermissions.Add(new RolePermission(staffRole.Id, permission.Id));
            }
        }

        dbContext.RolePermissions.AddRange(rolePermissions);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default roles with permissions", 4);
    }

    private async Task CreateOwnerUserAsync(
        IServiceScope scope,
        string schemaName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            _logger.LogDebug("Owner user already exists");
            return;
        }

        var ownerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Owner", cancellationToken);
        if (ownerRole == null)
        {
            throw new InvalidOperationException("Owner role not found");
        }

        var hashedPassword = passwordHasher.HashPassword(password);
        var user = User.Create(
            email: email,
            passwordHash: hashedPassword,
            firstName: "Account",
            lastName: "Owner");

        user.VerifyEmail();
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Assign owner role
        var userRole = new UserRole(user.Id, ownerRole.Id);
        dbContext.UserRoles.Add(userRole);

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created owner user {Email}", email);
    }

    private async Task CreateDefaultSettingsAsync(
        IServiceScope scope,
        string schemaName,
        string companyName,
        CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        if (await dbContext.TenantSettings.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Settings already exist");
            return;
        }

        var settings = TenantSettings.CreateDefault();
        dbContext.TenantSettings.Add(settings);

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created default settings for tenant");
    }
}
