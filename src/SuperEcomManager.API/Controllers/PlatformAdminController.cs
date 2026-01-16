using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Features.PlatformAdmin;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for platform administration operations.
/// Manages tenants, platform admins, and system-wide settings.
/// </summary>
[ApiController]
[Route("api/platform-admin")]
[Authorize(Policy = "PlatformAdmin")]
public class PlatformAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Tenant Management

    /// <summary>
    /// Get list of tenants with filtering and pagination.
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? searchTerm,
        [FromQuery] TenantStatus? status,
        [FromQuery] string? planCode,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] bool? isTrialActive,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTenantsQuery
        {
            SearchTerm = searchTerm,
            Status = status,
            PlanCode = planCode,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            IsTrialActive = isTrialActive,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get tenant details by ID.
    /// </summary>
    [HttpGet("tenants/{tenantId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid tenantId)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery { TenantId = tenantId });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new tenant.
    /// </summary>
    [HttpPost("tenants")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return CreatedAtAction(
            nameof(GetTenant),
            new { tenantId = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Suspend a tenant.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendTenant(Guid tenantId, [FromBody] SuspendTenantRequest request)
    {
        var result = await _mediator.Send(new SuspendTenantCommand
        {
            TenantId = tenantId,
            Reason = request.Reason
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Tenant suspended successfully" });
    }

    /// <summary>
    /// Reactivate a suspended tenant.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReactivateTenant(Guid tenantId, [FromBody] ReactivateTenantRequest? request)
    {
        var result = await _mediator.Send(new ReactivateTenantCommand
        {
            TenantId = tenantId,
            Notes = request?.Notes
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Tenant reactivated successfully" });
    }

    /// <summary>
    /// Deactivate a tenant permanently.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateTenant(Guid tenantId, [FromBody] DeactivateTenantRequest request)
    {
        var result = await _mediator.Send(new DeactivateTenantCommand
        {
            TenantId = tenantId,
            Reason = request.Reason,
            DeleteData = request.DeleteData
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Tenant deactivated successfully" });
    }

    /// <summary>
    /// Extend tenant's trial period.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/extend-trial")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendTrial(Guid tenantId, [FromBody] ExtendTrialRequest request)
    {
        var result = await _mediator.Send(new ExtendTrialCommand
        {
            TenantId = tenantId,
            AdditionalDays = request.AdditionalDays,
            Reason = request.Reason
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = $"Trial extended by {request.AdditionalDays} days" });
    }

    #endregion

    #region Activity Logs

    /// <summary>
    /// Get tenant activity logs.
    /// </summary>
    [HttpGet("activity-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetTenantActivityLogsQuery
        {
            TenantId = tenantId,
            Action = action,
            From = from,
            To = to,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        });

        return Ok(result);
    }

    /// <summary>
    /// Get activity logs for a specific tenant.
    /// </summary>
    [HttpGet("tenants/{tenantId:guid}/activity-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantActivityLogs(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetTenantActivityLogsQuery
        {
            TenantId = tenantId,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        });

        return Ok(result);
    }

    #endregion

    #region Platform Stats

    /// <summary>
    /// Get platform statistics and dashboard data.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformStats()
    {
        var result = await _mediator.Send(new GetPlatformStatsQuery());
        return Ok(result);
    }

    #endregion

    #region Platform Admin Management

    /// <summary>
    /// Get list of platform admins.
    /// </summary>
    [HttpGet("admins")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformAdmins(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isSuperAdmin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPlatformAdminsQuery
        {
            SearchTerm = searchTerm,
            IsActive = isActive,
            IsSuperAdmin = isSuperAdmin,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        });

        return Ok(result);
    }

    /// <summary>
    /// Get platform admin by ID.
    /// </summary>
    [HttpGet("admins/{adminId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlatformAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new GetPlatformAdminByIdQuery { AdminId = adminId });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new platform admin.
    /// </summary>
    [HttpPost("admins")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlatformAdmin([FromBody] CreatePlatformAdminRequest request)
    {
        var result = await _mediator.Send(new CreatePlatformAdminCommand
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password,
            IsSuperAdmin = request.IsSuperAdmin
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return CreatedAtAction(
            nameof(GetPlatformAdmin),
            new { adminId = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Update platform admin profile.
    /// </summary>
    [HttpPut("admins/{adminId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlatformAdmin(Guid adminId, [FromBody] UpdatePlatformAdminRequest request)
    {
        var result = await _mediator.Send(new UpdatePlatformAdminCommand
        {
            AdminId = adminId,
            FirstName = request.FirstName,
            LastName = request.LastName
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activate a platform admin.
    /// </summary>
    [HttpPost("admins/{adminId:guid}/activate")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivatePlatformAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new ActivatePlatformAdminCommand { AdminId = adminId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Platform admin activated successfully" });
    }

    /// <summary>
    /// Deactivate a platform admin.
    /// </summary>
    [HttpPost("admins/{adminId:guid}/deactivate")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivatePlatformAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new DeactivatePlatformAdminCommand { AdminId = adminId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Platform admin deactivated successfully" });
    }

    /// <summary>
    /// Delete a platform admin (soft delete).
    /// </summary>
    [HttpDelete("admins/{adminId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePlatformAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new DeletePlatformAdminCommand { AdminId = adminId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Platform admin deleted successfully" });
    }

    /// <summary>
    /// Promote platform admin to super admin.
    /// </summary>
    [HttpPost("admins/{adminId:guid}/promote")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PromoteToSuperAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new PromoteToSuperAdminCommand { AdminId = adminId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Platform admin promoted to super admin" });
    }

    /// <summary>
    /// Demote super admin.
    /// </summary>
    [HttpPost("admins/{adminId:guid}/demote")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DemoteFromSuperAdmin(Guid adminId)
    {
        var result = await _mediator.Send(new DemoteFromSuperAdminCommand { AdminId = adminId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Super admin demoted successfully" });
    }

    #endregion
}

#region Request DTOs

public record SuspendTenantRequest(string Reason);
public record ReactivateTenantRequest(string? Notes);
public record DeactivateTenantRequest(string Reason, bool DeleteData = false);
public record ExtendTrialRequest(int AdditionalDays, string? Reason);
public record CreatePlatformAdminRequest(string Email, string FirstName, string LastName, string Password, bool IsSuperAdmin = false);
public record UpdatePlatformAdminRequest(string FirstName, string LastName);

#endregion
