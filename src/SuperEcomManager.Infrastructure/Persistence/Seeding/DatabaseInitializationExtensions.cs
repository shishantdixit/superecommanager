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

            // Apply any pending schema updates (for existing tenants that can't run full migrations)
            await ApplyPendingSchemaUpdatesAsync(services, appDbContext, logger);

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
    /// Applies pending schema updates to existing tenant schemas.
    /// This handles cases where full migrations can't run (e.g., due to extension dependencies).
    /// </summary>
    private static async Task ApplyPendingSchemaUpdatesAsync(
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
            return;
        }

        logger.LogInformation("Checking for pending schema updates in {Count} tenant schemas...", tenants.Count);

        // SQL to add Shopify credential columns if they don't exist
        const string addShopifyCredentialsSql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'AccessToken') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""AccessToken"" text;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'ApiKey') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""ApiKey"" text;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'ApiSecret') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""ApiSecret"" text;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'IsConnected') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""IsConnected"" boolean NOT NULL DEFAULT false;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'LastConnectedAt') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""LastConnectedAt"" timestamp with time zone;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'LastError') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""LastError"" text;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'Scopes') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""Scopes"" text;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'InitialSyncDays') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""InitialSyncDays"" integer DEFAULT 7;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'SyncProductsEnabled') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""SyncProductsEnabled"" boolean NOT NULL DEFAULT false;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'AutoSyncProducts') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""AutoSyncProducts"" boolean NOT NULL DEFAULT false;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'LastProductSyncAt') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""LastProductSyncAt"" timestamp with time zone;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '{0}' AND table_name = 'sales_channels' AND column_name = 'LastInventorySyncAt') THEN
                    ALTER TABLE ""{0}"".sales_channels ADD COLUMN ""LastInventorySyncAt"" timestamp with time zone;
                END IF;
            END $$;
        ";

        foreach (var tenant in tenants)
        {
            try
            {
                var sql = string.Format(addShopifyCredentialsSql, tenant.SchemaName);
                await appDbContext.Database.ExecuteSqlRawAsync(sql);
                logger.LogDebug("Applied schema updates for tenant {Schema}", tenant.SchemaName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to apply schema updates for tenant {Schema}", tenant.SchemaName);
            }
        }

        logger.LogInformation("Pending schema updates applied");
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
