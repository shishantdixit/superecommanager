using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Persistence.Migrations;

/// <summary>
/// Service for managing database migrations and schema operations.
/// </summary>
public interface IMigrationService
{
    Task<MigrationStatus> GetMigrationStatusAsync(CancellationToken cancellationToken = default);
    Task<MigrationResult> ApplySharedMigrationsAsync(CancellationToken cancellationToken = default);
    Task<MigrationResult> ApplyTenantMigrationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<MigrationResult> ApplyAllTenantMigrationsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}

public class MigrationService : IMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IServiceProvider serviceProvider,
        IApplicationDbContext applicationDbContext,
        ILogger<MigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _applicationDbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<MigrationStatus> GetMigrationStatusAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var appliedMigrations = await appDbContext.Database
            .GetAppliedMigrationsAsync(cancellationToken);

        var pendingMigrations = await appDbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);

        var allTenants = await _applicationDbContext.Tenants
            .AsNoTracking()
            .Select(t => new { t.Id, t.Name, t.SchemaName, t.Status })
            .ToListAsync(cancellationToken);

        var tenantStatuses = new List<TenantMigrationStatus>();
        foreach (var tenant in allTenants)
        {
            tenantStatuses.Add(new TenantMigrationStatus
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                SchemaName = tenant.SchemaName,
                Status = tenant.Status.ToString()
            });
        }

        return new MigrationStatus
        {
            SharedSchemaApplied = appliedMigrations.ToList(),
            SharedSchemaPending = pendingMigrations.ToList(),
            TenantStatuses = tenantStatuses,
            LastChecked = DateTime.UtcNow
        };
    }

    public async Task<MigrationResult> ApplySharedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult { SchemaType = "Shared" };

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pendingMigrations = await appDbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);

            result.PendingBefore = pendingMigrations.ToList();

            if (!result.PendingBefore.Any())
            {
                result.Success = true;
                result.Message = "No pending migrations";
                return result;
            }

            _logger.LogInformation("Applying {Count} pending shared migrations", result.PendingBefore.Count);

            await appDbContext.Database.MigrateAsync(cancellationToken);

            result.Success = true;
            result.AppliedMigrations = result.PendingBefore;
            result.Message = $"Applied {result.AppliedMigrations.Count} migrations";

            _logger.LogInformation("Shared migrations applied successfully");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            _logger.LogError(ex, "Failed to apply shared migrations");
        }

        return result;
    }

    public async Task<MigrationResult> ApplyTenantMigrationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult { SchemaType = "Tenant", TenantId = tenantId };

        try
        {
            var tenant = await _applicationDbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant == null)
            {
                result.Success = false;
                result.Message = "Tenant not found";
                return result;
            }

            result.SchemaName = tenant.SchemaName;

            _logger.LogInformation(
                "Applying migrations for tenant {TenantId} schema {Schema}",
                tenantId, tenant.SchemaName);

            using var scope = _serviceProvider.CreateScope();
            var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

            // Set the tenant context
            var currentTenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
            // Note: This would need to be handled through a mechanism to set tenant context

            await tenantDbContext.Database.MigrateAsync(cancellationToken);

            result.Success = true;
            result.Message = "Tenant migrations applied successfully";

            _logger.LogInformation(
                "Tenant {TenantId} migrations applied successfully",
                tenantId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            _logger.LogError(ex, "Failed to apply tenant migrations for {TenantId}", tenantId);
        }

        return result;
    }

    public async Task<MigrationResult> ApplyAllTenantMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult { SchemaType = "AllTenants" };
        var tenantResults = new List<MigrationResult>();

        try
        {
            var tenants = await _applicationDbContext.Tenants
                .AsNoTracking()
                .Where(t => t.Status == Domain.Enums.TenantStatus.Active ||
                           t.Status == Domain.Enums.TenantStatus.Pending)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Applying migrations for {Count} tenants", tenants.Count);

            foreach (var tenant in tenants)
            {
                var tenantResult = await ApplyTenantMigrationsAsync(tenant.Id, cancellationToken);
                tenantResults.Add(tenantResult);
            }

            var successCount = tenantResults.Count(r => r.Success);
            var failedCount = tenantResults.Count(r => !r.Success);

            result.Success = failedCount == 0;
            result.Message = $"Processed {tenants.Count} tenants. Success: {successCount}, Failed: {failedCount}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            _logger.LogError(ex, "Failed to apply all tenant migrations");
        }

        return result;
    }

    public async Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMigrations = await appDbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);

        return pendingMigrations.ToList();
    }
}

/// <summary>
/// Status of database migrations.
/// </summary>
public class MigrationStatus
{
    public List<string> SharedSchemaApplied { get; set; } = new();
    public List<string> SharedSchemaPending { get; set; } = new();
    public List<TenantMigrationStatus> TenantStatuses { get; set; } = new();
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Migration status for a tenant.
/// </summary>
public class TenantMigrationStatus
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Result of a migration operation.
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SchemaType { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public Guid? TenantId { get; set; }
    public List<string> PendingBefore { get; set; } = new();
    public List<string> AppliedMigrations { get; set; } = new();
}
