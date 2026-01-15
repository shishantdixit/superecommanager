using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Users;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// User and team management endpoints.
/// </summary>
[Authorize]
public class UsersController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserListDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] bool? emailVerified = null,
        [FromQuery] DateTime? lastLoginFrom = null,
        [FromQuery] DateTime? lastLoginTo = null,
        [FromQuery] UserSortBy sortBy = UserSortBy.CreatedAt,
        [FromQuery] bool sortDescending = true)
    {
        var result = await Mediator.Send(new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive,
            RoleId = roleId,
            EmailVerified = emailVerified,
            LastLoginFrom = lastLoginFrom,
            LastLoginTo = lastLoginTo,
            SortBy = sortBy,
            SortDescending = sortDescending
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<UserListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get user details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUser(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery { UserId = id });

        if (result.IsFailure)
            return NotFoundResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update user profile.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserProfileDto request)
    {
        var result = await Mediator.Send(new UpdateUserCommand
        {
            UserId = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone
        });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Activate a user.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> ActivateUser(Guid id)
    {
        var result = await Mediator.Send(new ActivateUserCommand { UserId = id });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Deactivate a user.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> DeactivateUser(Guid id)
    {
        var result = await Mediator.Send(new DeactivateUserCommand { UserId = id });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Unlock a locked-out user.
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> UnlockUser(Guid id)
    {
        var result = await Mediator.Send(new UnlockUserCommand { UserId = id });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Invite a new user to the tenant.
    /// </summary>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> InviteUser(
        [FromBody] CreateInvitationDto request)
    {
        var result = await Mediator.Send(new InviteUserCommand
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleIds = request.RoleIds,
            InvitedBy = CurrentUserId,
            SendInvitationEmail = true
        });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return CreatedResponse($"/api/users/{result.Value!.Id}", result.Value!);
    }

    /// <summary>
    /// Delete a user (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        var result = await Mediator.Send(new DeleteUserCommand
        {
            UserId = id,
            DeletedBy = CurrentUserId
        });

        if (result.IsFailure)
            return BadRequestResponse<bool>(string.Join(", ", result.Errors));

        return OkResponse(true);
    }

    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request)
    {
        var result = await Mediator.Send(new AssignRoleCommand
        {
            UserId = id,
            RoleId = request.RoleId,
            AssignedBy = CurrentUserId
        });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Remove a role from a user.
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> RemoveRole(Guid id, Guid roleId)
    {
        var result = await Mediator.Send(new RemoveRoleCommand
        {
            UserId = id,
            RoleId = roleId
        });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Set all roles for a user (replaces existing roles).
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> SetUserRoles(
        Guid id,
        [FromBody] SetUserRolesRequest request)
    {
        var result = await Mediator.Send(new SetUserRolesCommand
        {
            UserId = id,
            RoleIds = request.RoleIds,
            AssignedBy = CurrentUserId
        });

        if (result.IsFailure)
            return BadRequestResponse<UserDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get user statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<UserStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetUserStats()
    {
        var result = await Mediator.Send(new GetUserStatsQuery());

        if (result.IsFailure)
            return BadRequestResponse<UserStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get all available permissions grouped by module.
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiResponse<PermissionListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PermissionListDto>>> GetPermissions()
    {
        var result = await Mediator.Send(new GetPermissionsQuery());

        if (result.IsFailure)
            return BadRequestResponse<PermissionListDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}

/// <summary>
/// Request DTO for assigning a role.
/// </summary>
public record AssignRoleRequest
{
    public Guid RoleId { get; init; }
}

/// <summary>
/// Request DTO for setting user roles.
/// </summary>
public record SetUserRolesRequest
{
    public List<Guid> RoleIds { get; init; } = new();
}
