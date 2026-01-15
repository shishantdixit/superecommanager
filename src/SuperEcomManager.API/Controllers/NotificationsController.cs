using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Notifications;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Notification management endpoints.
/// </summary>
[Authorize]
public class NotificationsController : ApiControllerBase
{
    #region Templates

    /// <summary>
    /// Get paginated list of notification templates.
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationTemplateListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NotificationTemplateListDto>>>> GetTemplates(
        [FromQuery] NotificationType? type,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isSystem,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetNotificationTemplatesQuery
        {
            Filter = new NotificationTemplateFilterDto
            {
                Type = type,
                IsActive = isActive,
                IsSystem = isSystem,
                SearchTerm = searchTerm
            },
            Page = page,
            PageSize = pageSize
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<NotificationTemplateListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get notification template by ID.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDetailDto>>> GetTemplateById(Guid id)
    {
        var result = await Mediator.Send(new GetNotificationTemplateByIdQuery { TemplateId = id });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NotificationTemplateDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NotificationTemplateDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Create a new notification template.
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDetailDto>>> CreateTemplate(
        [FromBody] CreateTemplateRequest request)
    {
        var result = await Mediator.Send(new CreateNotificationTemplateCommand
        {
            Code = request.Code,
            Name = request.Name,
            Type = request.Type,
            Subject = request.Subject,
            Body = request.Body,
            Variables = request.Variables
        });

        if (result.IsFailure)
            return BadRequestResponse<NotificationTemplateDetailDto>(string.Join(", ", result.Errors));

        return CreatedResponse($"/api/notifications/templates/{result.Value!.Id}", result.Value!);
    }

    /// <summary>
    /// Update a notification template.
    /// </summary>
    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDetailDto>>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateTemplateRequest request)
    {
        var result = await Mediator.Send(new UpdateNotificationTemplateCommand
        {
            TemplateId = id,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NotificationTemplateDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NotificationTemplateDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Toggle template active status.
    /// </summary>
    [HttpPatch("templates/{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateListDto>>> ToggleTemplate(
        Guid id,
        [FromBody] ToggleTemplateRequest request)
    {
        var result = await Mediator.Send(new ToggleNotificationTemplateCommand
        {
            TemplateId = id,
            IsActive = request.IsActive
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NotificationTemplateListDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NotificationTemplateListDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    #endregion

    #region Logs

    /// <summary>
    /// Get paginated list of notification logs.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationLogListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NotificationLogListDto>>>> GetLogs(
        [FromQuery] NotificationType? type,
        [FromQuery] string? status,
        [FromQuery] string? recipient,
        [FromQuery] string? referenceType,
        [FromQuery] string? referenceId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetNotificationLogsQuery
        {
            Filter = new NotificationLogFilterDto
            {
                Type = type,
                Status = status,
                Recipient = recipient,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                FromDate = fromDate,
                ToDate = toDate
            },
            Page = page,
            PageSize = pageSize
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<NotificationLogListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get notification log by ID.
    /// </summary>
    [HttpGet("logs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationLogDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationLogDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationLogDetailDto>>> GetLogById(Guid id)
    {
        var result = await Mediator.Send(new GetNotificationLogByIdQuery { LogId = id });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NotificationLogDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NotificationLogDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    #endregion

    #region Send

    /// <summary>
    /// Send a notification.
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<SendNotificationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SendNotificationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SendNotificationResultDto>>> SendNotification(
        [FromBody] SendNotificationRequest request)
    {
        var result = await Mediator.Send(new SendNotificationCommand
        {
            Type = request.Type,
            Recipient = request.Recipient,
            TemplateCode = request.TemplateCode,
            Subject = request.Subject,
            Content = request.Content,
            Variables = request.Variables,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId
        });

        if (result.IsFailure)
            return BadRequestResponse<SendNotificationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Send bulk notifications.
    /// </summary>
    [HttpPost("send/bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<SendNotificationResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SendNotificationResultDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<SendNotificationResultDto>>>> SendBulkNotification(
        [FromBody] SendBulkNotificationRequest request)
    {
        var result = await Mediator.Send(new SendBulkNotificationCommand
        {
            Type = request.Type,
            Recipients = request.Recipients,
            TemplateCode = request.TemplateCode,
            Subject = request.Subject,
            Content = request.Content,
            Variables = request.Variables,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId
        });

        if (result.IsFailure)
            return BadRequestResponse<List<SendNotificationResultDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    #endregion

    #region Stats

    /// <summary>
    /// Get notification statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<NotificationStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetStats(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool includeDailyStats = true,
        [FromQuery] int dailyStatsDays = 30)
    {
        var result = await Mediator.Send(new GetNotificationStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            IncludeDailyStats = includeDailyStats,
            DailyStatsDays = dailyStatsDays
        });

        if (result.IsFailure)
            return BadRequestResponse<NotificationStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    #endregion
}

#region Request DTOs

public record CreateTemplateRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<string>? Variables { get; init; }
}

public record UpdateTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
}

public record ToggleTemplateRequest
{
    public bool IsActive { get; init; }
}

public record SendNotificationRequest
{
    public NotificationType Type { get; init; }
    public string Recipient { get; init; } = string.Empty;
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Content { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}

public record SendBulkNotificationRequest
{
    public NotificationType Type { get; init; }
    public List<string> Recipients { get; init; } = new();
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Content { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}

#endregion
