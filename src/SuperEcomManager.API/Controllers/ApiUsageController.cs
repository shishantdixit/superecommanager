using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.RateLimiting;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for viewing API usage statistics.
/// </summary>
[ApiController]
[Route("api/usage")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
public class ApiUsageController : ControllerBase
{
    private readonly IApiUsageTracker _usageTracker;
    private readonly ICurrentTenantService _tenantService;

    public ApiUsageController(IApiUsageTracker usageTracker, ICurrentTenantService tenantService)
    {
        _usageTracker = usageTracker;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Get API usage statistics for the current tenant.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiUsageStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageStats([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId == Guid.Empty ? "unknown" : _tenantService.TenantId.ToString();
        var stats = await _usageTracker.GetUsageStatsAsync(tenantId, Math.Min(days, 90));
        return Ok(stats);
    }

    /// <summary>
    /// Get API usage for a specific date.
    /// </summary>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyUsage([FromQuery] DateTime? date = null)
    {
        var tenantId = _tenantService.TenantId == Guid.Empty ? "unknown" : _tenantService.TenantId.ToString();
        var targetDate = date ?? DateTime.UtcNow.Date;
        var usage = await _usageTracker.GetDailyUsageAsync(tenantId, targetDate);
        return Ok(new
        {
            Date = targetDate.Date,
            TenantId = tenantId,
            EndpointUsage = usage
        });
    }
}
