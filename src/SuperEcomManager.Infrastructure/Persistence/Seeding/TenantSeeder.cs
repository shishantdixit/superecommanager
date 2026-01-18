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

        // Create a scope and set tenant context BEFORE resolving TenantDbContext
        using var scope = _serviceProvider.CreateScope();

        // Set tenant context - this is critical for TenantDbContext to use the correct schema
        var currentTenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        var slug = schemaName.StartsWith("tenant_") ? schemaName.Substring(7) : schemaName;
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        // Create schema and apply migrations
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
        // Create schema using ApplicationDbContext (not tenant-specific)
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await appDbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"",
            cancellationToken);

        // Tenant context must already be set before resolving TenantDbContext
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        // Open the connection explicitly so SET search_path persists for MigrateAsync
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            // Set PostgreSQL search_path to the tenant schema before applying migrations
            // This ensures unqualified table names in migrations are created in the correct schema
            await dbContext.Database.ExecuteSqlRawAsync(
                $"SET search_path TO \"{schemaName}\", public",
                cancellationToken);

            // Apply migrations to the tenant schema (uses the same connection)
            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogDebug("Created schema {Schema} and applied migrations", schemaName);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task SeedDefaultRolesAsync(IServiceScope scope, string schemaName, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Always sync permissions from shared schema to tenant schema (add any missing ones)
        var existingPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet();
        var sharedPermissions = await appDbContext.Permissions.ToListAsync(cancellationToken);
        var newPermissions = sharedPermissions.Where(p => !existingCodes.Contains(p.Code)).ToList();

        if (newPermissions.Count > 0)
        {
            foreach (var perm in newPermissions)
            {
                var tenantPerm = Permission.Create(perm.Code, perm.Name, perm.Module, perm.Description);
                dbContext.Permissions.Add(tenantPerm);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Synced {Count} new permissions to tenant schema", newPermissions.Count);

            // Add new permissions to Owner role
            var existingOwnerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Owner", cancellationToken);
            if (existingOwnerRole != null)
            {
                var tenantNewPerms = await dbContext.Permissions
                    .Where(p => newPermissions.Select(np => np.Code).Contains(p.Code))
                    .ToListAsync(cancellationToken);
                foreach (var perm in tenantNewPerms)
                {
                    dbContext.RolePermissions.Add(new RolePermission(existingOwnerRole.Id, perm.Id));
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Added {Count} new permissions to Owner role", tenantNewPerms.Count);
            }
        }

        if (await dbContext.Roles.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Roles already exist for tenant, skipping role creation");
            return;
        }

        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);
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
