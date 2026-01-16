using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Bulk;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Bulk operations and data export/import endpoints.
/// </summary>
[Authorize]
public class BulkController : ApiControllerBase
{
    #region Orders

    /// <summary>
    /// Update multiple orders at once.
    /// </summary>
    [HttpPost("orders/update")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkOperationResultDto>>> BulkUpdateOrders(
        [FromBody] BulkOrderUpdateRequestDto request)
    {
        var result = await Mediator.Send(new BulkUpdateOrdersCommand
        {
            OrderIds = request.OrderIds,
            NewStatus = request.NewStatus,
            InternalNotes = request.InternalNotes
        });

        if (result.IsFailure)
            return BadRequestResponse<BulkOperationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Cancel multiple orders at once.
    /// </summary>
    [HttpPost("orders/cancel")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkOperationResultDto>>> BulkCancelOrders(
        [FromBody] BulkCancelOrdersRequest request)
    {
        var result = await Mediator.Send(new BulkCancelOrdersCommand
        {
            OrderIds = request.OrderIds,
            CancellationReason = request.CancellationReason
        });

        if (result.IsFailure)
            return BadRequestResponse<BulkOperationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Export orders to CSV.
    /// </summary>
    [HttpGet("orders/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? channelId = null,
        [FromQuery] int? limit = 10000)
    {
        var result = await Mediator.Send(new ExportOrdersQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status,
            ChannelFilter = channelId,
            Limit = limit
        });

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(string.Join(", ", result.Errors)));

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    #endregion

    #region Shipments

    /// <summary>
    /// Create shipments for multiple orders at once.
    /// </summary>
    [HttpPost("shipments/create")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkOperationResultDto>>> BulkCreateShipments(
        [FromBody] BulkShipmentCreateRequestDto request)
    {
        var result = await Mediator.Send(new BulkCreateShipmentsCommand
        {
            OrderIds = request.OrderIds,
            CourierType = request.CourierType,
            CourierAccountId = request.CourierAccountId
        });

        if (result.IsFailure)
            return BadRequestResponse<BulkOperationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Export shipments to CSV.
    /// </summary>
    [HttpGet("shipments/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportShipments(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] CourierType? courier = null,
        [FromQuery] int? limit = 10000)
    {
        var result = await Mediator.Send(new ExportShipmentsQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status,
            CourierFilter = courier,
            Limit = limit
        });

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(string.Join(", ", result.Errors)));

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    #endregion

    #region NDR

    /// <summary>
    /// Assign multiple NDR cases to an agent.
    /// </summary>
    [HttpPost("ndr/assign")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkOperationResultDto>>> BulkAssignNdr(
        [FromBody] BulkNdrAssignRequestDto request)
    {
        var result = await Mediator.Send(new BulkAssignNdrCommand
        {
            NdrIds = request.NdrIds,
            AssignToUserId = request.AssignToUserId
        });

        if (result.IsFailure)
            return BadRequestResponse<BulkOperationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update status of multiple NDR cases.
    /// </summary>
    [HttpPost("ndr/status")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkOperationResultDto>>> BulkUpdateNdrStatus(
        [FromBody] BulkNdrStatusUpdateRequestDto request)
    {
        var result = await Mediator.Send(new BulkUpdateNdrStatusCommand
        {
            NdrIds = request.NdrIds,
            NewStatus = request.NewStatus,
            Remarks = request.Remarks
        });

        if (result.IsFailure)
            return BadRequestResponse<BulkOperationResultDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    #endregion

    #region Products

    /// <summary>
    /// Export products/inventory to CSV.
    /// </summary>
    [HttpGet("products/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] bool outOfStockOnly = false,
        [FromQuery] int? limit = 10000)
    {
        var result = await Mediator.Send(new ExportProductsQuery
        {
            IncludeLowStock = lowStockOnly,
            IncludeOutOfStock = outOfStockOnly,
            Limit = limit
        });

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(string.Join(", ", result.Errors)));

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    #endregion
}

/// <summary>
/// Request to cancel multiple orders.
/// </summary>
public record BulkCancelOrdersRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public string? CancellationReason { get; init; }
}
