using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Ndr;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// NDR (Non-Delivery Report) management endpoints.
/// </summary>
[Authorize]
public class NdrController : ApiControllerBase
{
    /// <summary>
    /// Get a paginated list of NDR cases with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NdrListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NdrListDto>>>> GetNdrCases(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? orderId,
        [FromQuery] Guid? shipmentId,
        [FromQuery] NdrStatus? status,
        [FromQuery] NdrReasonCode? reasonCode,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] bool? unassigned,
        [FromQuery] bool? hasFollowUpDue,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NdrSortBy sortBy = NdrSortBy.NdrDate,
        [FromQuery] bool sortDescending = true)
    {
        var result = await Mediator.Send(new GetNdrCasesQuery
        {
            Filter = new NdrFilterDto
            {
                SearchTerm = searchTerm,
                OrderId = orderId,
                ShipmentId = shipmentId,
                Status = status,
                ReasonCode = reasonCode,
                AssignedToUserId = assignedToUserId,
                Unassigned = unassigned,
                HasFollowUpDue = hasFollowUpDue,
                FromDate = fromDate,
                ToDate = toDate
            },
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<NdrListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get NDR case details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrDetailDto>>> GetNdrCaseById(Guid id)
    {
        var result = await Mediator.Send(new GetNdrCaseByIdQuery { NdrRecordId = id });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Assign an NDR case to a user.
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrDetailDto>>> AssignNdrCase(
        Guid id,
        [FromBody] AssignNdrCaseRequest request)
    {
        var result = await Mediator.Send(new AssignNdrCaseCommand
        {
            NdrRecordId = id,
            AssignToUserId = request.AssignToUserId,
            Remarks = request.Remarks
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Log an action (call, SMS, WhatsApp, Email) for an NDR case.
    /// </summary>
    [HttpPost("{id:guid}/actions")]
    [ProducesResponseType(typeof(ApiResponse<NdrActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrActionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NdrActionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrActionDto>>> LogNdrAction(
        Guid id,
        [FromBody] LogNdrActionRequest request)
    {
        var result = await Mediator.Send(new LogNdrActionCommand
        {
            NdrRecordId = id,
            ActionType = request.ActionType,
            Details = request.Details,
            Outcome = request.Outcome,
            CallDurationSeconds = request.CallDurationSeconds
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrActionDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrActionDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Add a remark/note to an NDR case.
    /// </summary>
    [HttpPost("{id:guid}/remarks")]
    [ProducesResponseType(typeof(ApiResponse<NdrRemarkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrRemarkDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NdrRemarkDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrRemarkDto>>> AddNdrRemark(
        Guid id,
        [FromBody] AddNdrRemarkRequest request)
    {
        var result = await Mediator.Send(new AddNdrRemarkCommand
        {
            NdrRecordId = id,
            Content = request.Content,
            IsInternal = request.IsInternal
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrRemarkDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrRemarkDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Schedule a reattempt for an NDR case.
    /// </summary>
    [HttpPost("{id:guid}/reattempt")]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrDetailDto>>> ScheduleReattempt(
        Guid id,
        [FromBody] ScheduleReattemptRequest request)
    {
        var result = await Mediator.Send(new ScheduleReattemptCommand
        {
            NdrRecordId = id,
            ReattemptDate = request.ReattemptDate,
            UpdatedAddress = request.UpdatedAddress,
            Remarks = request.Remarks
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update the outcome/resolution of an NDR case.
    /// </summary>
    [HttpPut("{id:guid}/outcome")]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NdrDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NdrDetailDto>>> UpdateNdrOutcome(
        Guid id,
        [FromBody] UpdateNdrOutcomeRequest request)
    {
        var result = await Mediator.Send(new UpdateNdrOutcomeCommand
        {
            NdrRecordId = id,
            NewStatus = request.NewStatus,
            Resolution = request.Resolution
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<NdrDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<NdrDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get NDR statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<NdrStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NdrStatsDto>>> GetNdrStats(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await Mediator.Send(new GetNdrStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });

        if (result.IsFailure)
            return BadRequestResponse<NdrStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get NDR cases for a specific shipment.
    /// </summary>
    [HttpGet("by-shipment/{shipmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NdrListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NdrListDto>>>> GetNdrCasesByShipment(
        Guid shipmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetNdrCasesQuery
        {
            Filter = new NdrFilterDto { ShipmentId = shipmentId },
            Page = page,
            PageSize = pageSize,
            SortBy = NdrSortBy.NdrDate,
            SortDescending = true
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<NdrListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get NDR cases assigned to the current user.
    /// </summary>
    [HttpGet("my-cases")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NdrListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NdrListDto>>>> GetMyNdrCases(
        [FromQuery] NdrStatus? status,
        [FromQuery] bool? hasFollowUpDue,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Current user ID will be resolved in the handler via ICurrentUserService
        var result = await Mediator.Send(new GetNdrCasesQuery
        {
            Filter = new NdrFilterDto
            {
                Status = status,
                HasFollowUpDue = hasFollowUpDue
                // AssignedToUserId will be set to current user in the controller below
            },
            Page = page,
            PageSize = pageSize,
            SortBy = NdrSortBy.NextFollowUpAt,
            SortDescending = false
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<NdrListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}

// Request DTOs for the controller
public record AssignNdrCaseRequest
{
    public Guid AssignToUserId { get; init; }
    public string? Remarks { get; init; }
}

public record LogNdrActionRequest
{
    public NdrActionType ActionType { get; init; }
    public string? Details { get; init; }
    public string? Outcome { get; init; }
    public int? CallDurationSeconds { get; init; }
}

public record AddNdrRemarkRequest
{
    public string Content { get; init; } = string.Empty;
    public bool IsInternal { get; init; } = true;
}

public record ScheduleReattemptRequest
{
    public DateTime ReattemptDate { get; init; }
    public AddressDto? UpdatedAddress { get; init; }
    public string? Remarks { get; init; }
}

public record UpdateNdrOutcomeRequest
{
    public NdrStatus NewStatus { get; init; }
    public string? Resolution { get; init; }
}
