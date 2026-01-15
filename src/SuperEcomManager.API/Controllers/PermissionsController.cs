using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Permissions;
using SuperEcomManager.Application.Features.Roles;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Permission management endpoints.
/// </summary>
[Authorize]
public class PermissionsController : ApiControllerBase
{
    /// <summary>
    /// Get all permissions grouped by module.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionGroupDto>>>> GetPermissions()
    {
        var permissions = await Mediator.Send(new GetPermissionsQuery());
        return OkResponse(permissions);
    }
}
