using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Roles;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Role management endpoints.
/// </summary>
[Authorize]
public class RolesController : ApiControllerBase
{
    /// <summary>
    /// Get all roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleSummaryDto>>>> GetRoles()
    {
        var roles = await Mediator.Send(new GetRolesQuery());
        return OkResponse(roles);
    }

    /// <summary>
    /// Get role by ID with permissions.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(Guid id)
    {
        var role = await Mediator.Send(new GetRoleByIdQuery(id));
        return OkResponse(role);
    }

    /// <summary>
    /// Create a new role.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var command = new CreateRoleCommand
        {
            Name = request.Name,
            Description = request.Description,
            PermissionIds = request.PermissionIds
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Failed to create role"
            });
        }

        return CreatedResponse($"/api/roles/{result.Value!.Id}", result.Value!, "Role created successfully");
    }

    /// <summary>
    /// Update an existing role.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var command = new UpdateRoleCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Failed to update role"
            });
        }

        return OkResponse(result.Value!, "Role updated successfully");
    }

    /// <summary>
    /// Delete a role.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(Guid id)
    {
        var result = await Mediator.Send(new DeleteRoleCommand(id));

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Failed to delete role"
            });
        }

        return OkResponse(true, "Role deleted successfully");
    }

    /// <summary>
    /// Assign permissions to a role.
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> AssignPermissions(Guid id, [FromBody] AssignPermissionsRequest request)
    {
        var command = new AssignPermissionsCommand
        {
            RoleId = id,
            PermissionIds = request.PermissionIds
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Failed to assign permissions"
            });
        }

        return OkResponse(result.Value!, "Permissions assigned successfully");
    }
}

// Request DTOs
public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}

public record UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record AssignPermissionsRequest
{
    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}
