using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Shipments;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for shipment management operations.
/// </summary>
[Authorize]
public class ShipmentsController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of shipments with filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ShipmentListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ShipmentListDto>>>> GetShipments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] CourierType? courierType = null,
        [FromQuery] bool? isCOD = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] ShipmentSortBy sortBy = ShipmentSortBy.CreatedAt,
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetShipmentsQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Filter = new ShipmentFilterDto
            {
                SearchTerm = searchTerm,
                OrderId = orderId,
                Status = status,
                CourierType = courierType,
                IsCOD = isCOD,
                FromDate = fromDate,
                ToDate = toDate,
                City = city,
                State = state
            }
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<ShipmentListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get shipment details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShipmentDetailDto>>> GetShipment(Guid id)
    {
        var result = await Mediator.Send(new GetShipmentByIdQuery { ShipmentId = id });

        if (result.IsFailure)
            return NotFoundResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get shipment statistics for dashboard.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShipmentStatsDto>>> GetShipmentStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetShipmentStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<ShipmentStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get shipment tracking information.
    /// </summary>
    [HttpGet("{id:guid}/tracking")]
    [ProducesResponseType(typeof(ApiResponse<TrackingInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TrackingInfoDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrackingInfoDto>>> GetShipmentTracking(Guid id)
    {
        var result = await Mediator.Send(new GetShipmentTrackingQuery
        {
            ShipmentId = id
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<TrackingInfoDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<TrackingInfoDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Create a new shipment for an order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ShipmentDetailDto>>> CreateShipment(
        [FromBody] CreateShipmentRequest request)
    {
        Guid? courierAccountId = null;
        CourierType? courierType = null;

        // Try to parse as GUID (courier account ID) first
        if (Guid.TryParse(request.CourierCode, out var accountId))
        {
            courierAccountId = accountId;
        }
        // Otherwise, try to parse as CourierType enum for backward compatibility
        else if (Enum.TryParse<CourierType>(request.CourierCode, true, out var parsedCourierType))
        {
            courierType = parsedCourierType;
        }
        else
        {
            return BadRequestResponse<ShipmentDetailDto>($"Invalid courier code: {request.CourierCode}");
        }

        var command = new CreateShipmentCommand
        {
            OrderId = request.OrderId,
            CourierAccountId = courierAccountId,
            CourierType = courierType,
            PickupAddress = request.PickupAddress,
            Dimensions = new DimensionsDto
            {
                Weight = request.Weight,
                Length = request.Length ?? 10,
                Width = request.Width ?? 10,
                Height = request.Height ?? 10
            },
            ServiceCode = request.ServiceCode,
            Items = request.Items
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
            return BadRequestResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));

        return CreatedAtAction(
            nameof(GetShipment),
            new { id = result.Value!.Id },
            ApiResponse<ShipmentDetailDto>.Ok(result.Value, "Shipment created successfully."));
    }

    /// <summary>
    /// Update shipment status.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ShipmentDetailDto>>> UpdateShipmentStatus(
        Guid id,
        [FromBody] UpdateShipmentStatusRequest request)
    {
        var command = new UpdateShipmentStatusCommand
        {
            ShipmentId = id,
            NewStatus = request.Status,
            Location = request.Location,
            Remarks = request.Remarks
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!, "Shipment status updated successfully.");
    }

    /// <summary>
    /// Cancel a shipment.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelShipment(
        Guid id,
        [FromBody] CancelShipmentRequest request)
    {
        var command = new CancelShipmentCommand
        {
            ShipmentId = id,
            Reason = request.Reason
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<bool>(string.Join(", ", result.Errors));

            return BadRequestResponse<bool>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value, "Shipment cancelled successfully.");
    }

    /// <summary>
    /// Advanced filter shipments with POST body.
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ShipmentListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ShipmentListDto>>>> FilterShipments(
        [FromBody] FilterShipmentsRequest request)
    {
        var query = new GetShipmentsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending,
            Filter = request.Filter
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<ShipmentListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get shipments for a specific order.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ShipmentListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ShipmentListDto>>>> GetShipmentsByOrder(Guid orderId)
    {
        var query = new GetShipmentsQuery
        {
            Page = 1,
            PageSize = 100,
            Filter = new ShipmentFilterDto { OrderId = orderId }
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<List<ShipmentListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!.Items.ToList());
    }

    /// <summary>
    /// Get available couriers for a shipment (serviceability check).
    /// Returns list of couriers that can deliver to the shipment's destination.
    /// </summary>
    [HttpGet("{id:guid}/available-couriers")]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableCourierDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableCourierDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableCourierDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<AvailableCourierDto>>>> GetAvailableCouriers(Guid id)
    {
        var query = new GetAvailableCouriersQuery { ShipmentId = id };
        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<List<AvailableCourierDto>>(string.Join(", ", result.Errors));

            return BadRequestResponse<List<AvailableCourierDto>>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Assign a courier to a shipment.
    /// Used for shipments that were created in the courier system but don't have AWB assigned yet.
    /// </summary>
    [HttpPost("{id:guid}/assign-courier")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ShipmentDetailDto>>> AssignCourier(
        Guid id,
        [FromBody] AssignCourierRequest request)
    {
        var command = new AssignCourierCommand
        {
            ShipmentId = id,
            CourierId = request.CourierId
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<ShipmentDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!, "Courier assigned successfully.");
    }
}

#region Request Models

/// <summary>
/// Request model for creating a shipment.
/// </summary>
public record CreateShipmentRequest
{
    public Guid OrderId { get; init; }
    public string CourierCode { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }
    public AddressDto? PickupAddress { get; init; }
    public string? PickupDate { get; init; }
    public string? ServiceCode { get; init; }
    public List<CreateShipmentItemDto>? Items { get; init; }
}

/// <summary>
/// Request model for updating shipment status.
/// </summary>
public record UpdateShipmentStatusRequest
{
    public ShipmentStatus Status { get; init; }
    public string? Location { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>
/// Request model for cancelling a shipment.
/// </summary>
public record CancelShipmentRequest
{
    public string? Reason { get; init; }
}

/// <summary>
/// Request model for advanced shipment filtering.
/// </summary>
public record FilterShipmentsRequest
{
    public ShipmentFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ShipmentSortBy SortBy { get; init; } = ShipmentSortBy.CreatedAt;
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Request model for assigning a courier to a shipment.
/// </summary>
public record AssignCourierRequest
{
    /// <summary>
    /// The courier ID to assign. If null, Shiprocket will auto-select the recommended courier.
    /// </summary>
    public int? CourierId { get; init; }
}

#endregion
