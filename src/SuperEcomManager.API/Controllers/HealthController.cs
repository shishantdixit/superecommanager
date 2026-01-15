using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Health check endpoints for monitoring and container orchestration.
/// </summary>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext dbContext,
        ICacheService cacheService,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check with dependency status.
    /// </summary>
    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var health = new HealthStatus
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Checks = new Dictionary<string, CheckResult>()
        };

        // Check database
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            health.Checks["database"] = new CheckResult { Status = "healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            health.Checks["database"] = new CheckResult { Status = "unhealthy", Error = ex.Message };
            health.Status = "unhealthy";
        }

        // Check Redis
        try
        {
            var testKey = "__health_check__";
            await _cacheService.SetAsync(testKey, "ok", TimeSpan.FromSeconds(5), cancellationToken);
            var result = await _cacheService.GetAsync<string>(testKey, cancellationToken);
            await _cacheService.RemoveAsync(testKey, cancellationToken);

            health.Checks["redis"] = result == "ok"
                ? new CheckResult { Status = "healthy" }
                : new CheckResult { Status = "unhealthy", Error = "Cache read/write mismatch" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            health.Checks["redis"] = new CheckResult { Status = "unhealthy", Error = ex.Message };
            health.Status = "degraded";
        }

        var statusCode = health.Status == "healthy" ? 200 :
                         health.Status == "degraded" ? 200 : 503;

        return StatusCode(statusCode, health);
    }

    /// <summary>
    /// Liveness probe for Kubernetes.
    /// </summary>
    [HttpGet("/health/live")]
    public IActionResult Live()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

public class HealthStatus
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public Dictionary<string, CheckResult> Checks { get; set; } = new();
}

public class CheckResult
{
    public string Status { get; set; } = "healthy";
    public string? Error { get; set; }
}
