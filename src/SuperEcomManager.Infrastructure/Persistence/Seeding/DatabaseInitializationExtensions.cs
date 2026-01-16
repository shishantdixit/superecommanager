using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Extension methods for database initialization.
/// </summary>
public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Initializes the database with migrations and seed data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            // Apply shared schema migrations
            var appDbContext = services.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Applying shared schema migrations...");
            await appDbContext.Database.MigrateAsync();
            logger.LogInformation("Shared schema migrations applied");

            // Apply tenant migrations for all existing tenants
            await ApplyTenantMigrationsAsync(services, appDbContext, logger);

            // Seed shared data
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedSharedDataAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    /// <summary>
    /// Applies pending migrations to all tenant schemas.
    /// </summary>
    private static async Task ApplyTenantMigrationsAsync(
        IServiceProvider services,
        ApplicationDbContext appDbContext,
        ILogger logger)
    {
        var tenants = await appDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Status == Domain.Enums.TenantStatus.Active ||
                       t.Status == Domain.Enums.TenantStatus.Pending)
            .Select(t => new { t.Id, t.SchemaName, t.Slug })
            .ToListAsync();

        if (tenants.Count == 0)
        {
            logger.LogDebug("No tenants found for migration");
            return;
        }

        logger.LogInformation("Applying tenant migrations for {Count} tenants...", tenants.Count);

        foreach (var tenant in tenants)
        {
            try
            {
                // Create a new scope for each tenant to get fresh TenantDbContext
                using var tenantScope = services.CreateScope();
                var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();

                // Set the tenant context BEFORE resolving TenantDbContext
                currentTenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

                var tenantDbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();

                // Open connection explicitly so search_path persists for MigrateAsync
                await tenantDbContext.Database.OpenConnectionAsync();
                try
                {
                    // Set PostgreSQL search_path to the tenant schema before applying migrations
                    await tenantDbContext.Database.ExecuteSqlRawAsync(
                        $"SET search_path TO \"{tenant.SchemaName}\", public");

                    // Check for pending migrations
                    var pendingMigrations = await tenantDbContext.Database.GetPendingMigrationsAsync();
                    var pendingList = pendingMigrations.ToList();

                    if (pendingList.Count > 0)
                    {
                        logger.LogInformation(
                            "Applying {Count} pending migrations for tenant {Schema}: {Migrations}",
                            pendingList.Count, tenant.SchemaName, string.Join(", ", pendingList));
                    }

                    // Apply migrations (this will create schema and tables if needed)
                    await tenantDbContext.Database.MigrateAsync();

                    logger.LogDebug("Migrations completed for tenant schema {Schema}", tenant.SchemaName);
                }
                finally
                {
                    await tenantDbContext.Database.CloseConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to apply migrations for tenant schema {Schema}", tenant.SchemaName);
                // Continue with other tenants
            }
        }

        logger.LogInformation("Tenant migrations completed");
    }

    /// <summary>
    /// Ensures the database is created (for development only).
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            var appDbContext = services.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Ensuring database exists...");
            await appDbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating the database");
            throw;
        }
    }
}
