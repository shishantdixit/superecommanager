using MediatR;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Base controller for all API controllers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Gets the current authenticated user's ID from claims.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    /// <summary>
    /// Returns OK response with data.
    /// </summary>
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Returns OK response with paginated data.
    /// </summary>
    protected ActionResult<ApiResponse<IReadOnlyList<T>>> OkResponse<T>(PaginatedList<T> pagedData, string? message = null)
    {
        var pagination = PaginationMeta.FromPaginatedList(pagedData);
        return Ok(ApiResponse<IReadOnlyList<T>>.Ok(pagedData.Items, pagination, message));
    }

    /// <summary>
    /// Returns Created response with data.
    /// </summary>
    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(string location, T data, string? message = null)
    {
        return Created(location, ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Returns No Content response.
    /// </summary>
    protected new ActionResult NoContent()
    {
        return base.NoContent();
    }

    /// <summary>
    /// Returns Not Found response.
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string message)
    {
        return NotFound(ApiResponse<T>.Fail(message));
    }

    /// <summary>
    /// Returns Bad Request response.
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequestResponse<T>(string message, IEnumerable<string>? errors = null)
    {
        return BadRequest(ApiResponse<T>.Fail(message, errors));
    }
}
