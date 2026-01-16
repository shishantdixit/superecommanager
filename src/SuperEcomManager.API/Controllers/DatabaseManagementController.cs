using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Infrastructure.Persistence.Migrations;
using SuperEcomManager.Infrastructure.Persistence.Seeding;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for database management operations (Platform Admin only).
/// </summary>
[ApiController]
[Route("api/platform-admin/database")]
[Authorize(Policy = "SuperAdmin")]
public class DatabaseManagementController : ControllerBase
{
    private readonly IMigrationService _migrationService;
    private readonly DatabaseSeeder _databaseSeeder;

    public DatabaseManagementController(
        IMigrationService migrationService,
        DatabaseSeeder databaseSeeder)
    {
        _migrationService = migrationService;
        _databaseSeeder = databaseSeeder;
    }

    /// <summary>
    /// Get current migration status for all schemas.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MigrationStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _migrationService.GetMigrationStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Get list of pending migrations.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMigrations(CancellationToken cancellationToken)
    {
        var pending = await _migrationService.GetPendingMigrationsAsync(cancellationToken);
        return Ok(new
        {
            Count = pending.Count,
            Migrations = pending
        });
    }

    /// <summary>
    /// Apply pending migrations to shared schema.
    /// </summary>
    [HttpPost("migrate/shared")]
    [ProducesResponseType(typeof(MigrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplySharedMigrations(CancellationToken cancellationToken)
    {
        var result = await _migrationService.ApplySharedMigrationsAsync(cancellationToken);

        if (!result.Success)
        {
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Apply migrations for a specific tenant.
    /// </summary>
    [HttpPost("migrate/tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(MigrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyTenantMigrations(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _migrationService.ApplyTenantMigrationsAsync(tenantId, cancellationToken);

        if (!result.Success && result.Message == "Tenant not found")
        {
            return NotFound(result);
        }

        if (!result.Success)
        {
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Apply migrations for all active tenants.
    /// </summary>
    [HttpPost("migrate/all-tenants")]
    [ProducesResponseType(typeof(MigrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyAllTenantMigrations(CancellationToken cancellationToken)
    {
        var result = await _migrationService.ApplyAllTenantMigrationsAsync(cancellationToken);

        if (!result.Success)
        {
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Seed shared data (features, permissions, plans).
    /// </summary>
    [HttpPost("seed/shared")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedSharedData(CancellationToken cancellationToken)
    {
        try
        {
            await _databaseSeeder.SeedSharedDataAsync(cancellationToken);
            return Ok(new { message = "Shared data seeded successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run full database initialization (migrations + seeding).
    /// </summary>
    [HttpPost("initialize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InitializeDatabase(CancellationToken cancellationToken)
    {
        var results = new DatabaseInitializationResult();

        try
        {
            // Step 1: Apply shared migrations
            var migrationResult = await _migrationService.ApplySharedMigrationsAsync(cancellationToken);
            results.MigrationResult = migrationResult;

            if (!migrationResult.Success)
            {
                results.Success = false;
                results.Message = "Migration failed";
                return StatusCode(500, results);
            }

            // Step 2: Seed shared data
            await _databaseSeeder.SeedSharedDataAsync(cancellationToken);
            results.SeedingCompleted = true;

            results.Success = true;
            results.Message = "Database initialized successfully";

            return Ok(results);
        }
        catch (Exception ex)
        {
            results.Success = false;
            results.Message = ex.Message;
            return StatusCode(500, results);
        }
    }
}

/// <summary>
/// Result of database initialization.
/// </summary>
public class DatabaseInitializationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MigrationResult? MigrationResult { get; set; }
    public bool SeedingCompleted { get; set; }
}
