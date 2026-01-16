using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
