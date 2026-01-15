using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Audit;
using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Audit log and activity tracking endpoints.
/// </summary>
[Authorize]
public class AuditController : ApiControllerBase
{
    /// <summary>
    /// Get paginated audit logs with filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AuditLogListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AuditLogListDto>>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AuditModule? module = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAuditLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            Module = module,
            Action = action,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            IsSuccess = isSuccess,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<AuditLogListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get audit log details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuditLogDetailDto>>> GetAuditLog(Guid id)
    {
        var result = await Mediator.Send(new GetAuditLogByIdQuery { Id = id });

        if (result.IsFailure)
            return NotFoundResponse<AuditLogDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get login history with filters.
    /// </summary>
    [HttpGet("logins")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LoginHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<LoginHistoryDto>>>> GetLoginHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? ipAddress = null)
    {
        var result = await Mediator.Send(new GetLoginHistoryQuery
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            IsSuccess = isSuccess,
            FromDate = fromDate,
            ToDate = toDate,
            IpAddress = ipAddress
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<LoginHistoryDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get activity for a specific user.
    /// </summary>
    [HttpGet("users/{userId:guid}/activity")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserActivityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserActivityDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserActivityDto>>>> GetUserActivity(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AuditModule? module = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetUserActivityQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize,
            Module = module,
            FromDate = fromDate,
            ToDate = toDate
        });

        if (result.IsFailure)
            return NotFoundResponse<PaginatedResult<UserActivityDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get audit history for a specific entity.
    /// </summary>
    [HttpGet("entities/{entityType}/{entityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AuditLogListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AuditLogListDto>>>> GetEntityAuditHistory(
        string entityType,
        Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetEntityAuditHistoryQuery
        {
            EntityType = entityType,
            EntityId = entityId,
            Page = page,
            PageSize = pageSize
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<AuditLogListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get audit statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<AuditStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuditStatsDto>>> GetAuditStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetAuditStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });

        if (result.IsFailure)
            return BadRequestResponse<AuditStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}
