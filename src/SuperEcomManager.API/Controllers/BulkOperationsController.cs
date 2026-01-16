using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuperEcomManager.Application.Features.BulkOperations;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Infrastructure.RateLimiting;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for bulk operations including batch updates and exports.
/// </summary>
[ApiController]
[Route("api/bulk")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Bulk)]
public class BulkOperationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BulkOperationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Shipments

    /// <summary>
    /// Create shipments for multiple orders at once.
    /// </summary>
    [HttpPost("shipments")]
    [ProducesResponseType(typeof(BulkCreateShipmentsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateShipments([FromBody] BulkCreateShipmentsRequest request)
    {
        var result = await _mediator.Send(new BulkCreateShipmentsCommand
        {
            Orders = request.Orders.Select(o => new BulkShipmentInput
            {
                OrderId = o.OrderId,
                CourierType = o.CourierType
            }).ToList(),
            DefaultCourierType = request.DefaultCourierType
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update status for multiple shipments at once.
    /// </summary>
    [HttpPut("shipments/status")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpdateShipmentStatus([FromBody] BulkUpdateShipmentStatusRequest request)
    {
        var result = await _mediator.Send(new BulkUpdateShipmentStatusCommand
        {
            ShipmentIds = request.ShipmentIds,
            NewStatus = request.NewStatus,
            Remarks = request.Remarks
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel multiple shipments at once.
    /// </summary>
    [HttpPost("shipments/cancel")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCancelShipments([FromBody] BulkCancelShipmentsRequest request)
    {
        var result = await _mediator.Send(new BulkCancelShipmentsCommand
        {
            ShipmentIds = request.ShipmentIds,
            CancellationReason = request.Reason
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    #endregion

    #region Export

    /// <summary>
    /// Export orders data.
    /// </summary>
    [HttpPost("export/orders")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    [ProducesResponseType(typeof(BulkExportResult<OrderExportRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportOrders([FromBody] ExportOrdersRequest request)
    {
        var result = await _mediator.Send(new ExportOrdersCommand
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Status = request.Status,
            Statuses = request.Statuses,
            ChannelId = request.ChannelId,
            MaxRecords = request.MaxRecords
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Export shipments data.
    /// </summary>
    [HttpPost("export/shipments")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    [ProducesResponseType(typeof(BulkExportResult<ShipmentExportRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportShipments([FromBody] ExportShipmentsRequest request)
    {
        var result = await _mediator.Send(new ExportShipmentsCommand
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Status = request.Status,
            Statuses = request.Statuses,
            CourierType = request.CourierType,
            MaxRecords = request.MaxRecords
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Export products data.
    /// </summary>
    [HttpPost("export/products")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    [ProducesResponseType(typeof(BulkExportResult<ProductExportRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportProducts([FromBody] ExportProductsRequest request)
    {
        var result = await _mediator.Send(new ExportProductsCommand
        {
            Category = request.Category,
            IsActive = request.IsActive,
            MaxRecords = request.MaxRecords
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Export NDR records data.
    /// </summary>
    [HttpPost("export/ndr")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    [ProducesResponseType(typeof(BulkExportResult<NdrExportRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportNdr([FromBody] ExportNdrRequest request)
    {
        var result = await _mediator.Send(new ExportNdrCommand
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Status = request.Status,
            ReasonCode = request.ReasonCode,
            AssignedToUserId = request.AssignedToUserId,
            MaxRecords = request.MaxRecords
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    #endregion
}

#region Request DTOs

// Shipment requests
public record BulkCreateShipmentsRequest(
    List<BulkShipmentRequestItem> Orders,
    CourierType? DefaultCourierType = null);

public record BulkShipmentRequestItem(Guid OrderId, CourierType CourierType = CourierType.Shiprocket);

public record BulkUpdateShipmentStatusRequest(
    List<Guid> ShipmentIds,
    ShipmentStatus NewStatus,
    string? Remarks = null);

public record BulkCancelShipmentsRequest(List<Guid> ShipmentIds, string? Reason = null);

// Export requests
public record ExportOrdersRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    OrderStatus? Status = null,
    List<OrderStatus>? Statuses = null,
    Guid? ChannelId = null,
    int? MaxRecords = 10000);

public record ExportShipmentsRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ShipmentStatus? Status = null,
    List<ShipmentStatus>? Statuses = null,
    CourierType? CourierType = null,
    int? MaxRecords = 10000);

public record ExportProductsRequest(
    string? Category = null,
    bool? IsActive = null,
    int? MaxRecords = 10000);

public record ExportNdrRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    NdrStatus? Status = null,
    NdrReasonCode? ReasonCode = null,
    Guid? AssignedToUserId = null,
    int? MaxRecords = 10000);

#endregion
