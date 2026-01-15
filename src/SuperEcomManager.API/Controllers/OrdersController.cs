using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Orders;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for order management operations.
/// </summary>
[Authorize]
public class OrdersController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of orders with filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderListDto>>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? channelId = null,
        [FromQuery] ChannelType? channelType = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] PaymentStatus? paymentStatus = null,
        [FromQuery] FulfillmentStatus? fulfillmentStatus = null,
        [FromQuery] bool? isCOD = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] OrderSortBy sortBy = OrderSortBy.OrderDate,
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Filter = new OrderFilterDto
            {
                SearchTerm = searchTerm,
                ChannelId = channelId,
                ChannelType = channelType,
                Status = status,
                PaymentStatus = paymentStatus,
                FulfillmentStatus = fulfillmentStatus,
                IsCOD = isCOD,
                FromDate = fromDate,
                ToDate = toDate,
                City = city,
                State = state,
                MinAmount = minAmount,
                MaxAmount = maxAmount
            }
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<OrderListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get order details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetOrder(Guid id)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery { OrderId = id });

        if (result.IsFailure)
            return NotFoundResponse<OrderDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get order statistics for dashboard.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderStatsDto>>> GetOrderStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetOrderStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<OrderStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update order status.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = id,
            NewStatus = request.Status,
            Reason = request.Reason
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<OrderDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<OrderDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!, "Order status updated successfully.");
    }

    /// <summary>
    /// Bulk update order statuses.
    /// </summary>
    [HttpPost("bulk-update")]
    [ProducesResponseType(typeof(ApiResponse<BulkUpdateResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkUpdateResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BulkUpdateResult>>> BulkUpdateOrders(
        [FromBody] BulkUpdateOrdersRequest request)
    {
        var command = new BulkUpdateOrdersCommand
        {
            OrderIds = request.OrderIds,
            NewStatus = request.Status,
            Reason = request.Reason
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
            return BadRequestResponse<BulkUpdateResult>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!, $"Updated {result.Value!.SuccessCount} of {result.Value.TotalRequested} orders.");
    }

    /// <summary>
    /// Update order internal notes.
    /// </summary>
    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateOrderNotes(
        Guid id,
        [FromBody] UpdateOrderNotesRequest request)
    {
        var command = new UpdateOrderNotesCommand
        {
            OrderId = id,
            InternalNotes = request.InternalNotes
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<bool>(string.Join(", ", result.Errors));

            return BadRequestResponse<bool>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value, "Order notes updated successfully.");
    }

    /// <summary>
    /// Cancel an order.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request)
    {
        var command = new CancelOrderCommand
        {
            OrderId = id,
            Reason = request.Reason
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<bool>(string.Join(", ", result.Errors));

            return BadRequestResponse<bool>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value, "Order cancelled successfully.");
    }

    /// <summary>
    /// Advanced filter orders with POST body.
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderListDto>>>> FilterOrders(
        [FromBody] FilterOrdersRequest request)
    {
        var query = new GetOrdersQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending,
            Filter = request.Filter
        };

        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<OrderListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}

#region Request Models

/// <summary>
/// Request model for updating order status.
/// </summary>
public record UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Request model for bulk updating orders.
/// </summary>
public record BulkUpdateOrdersRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public OrderStatus Status { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Request model for updating order internal notes.
/// </summary>
public record UpdateOrderNotesRequest
{
    public string? InternalNotes { get; init; }
}

/// <summary>
/// Request model for cancelling an order.
/// </summary>
public record CancelOrderRequest
{
    public string? Reason { get; init; }
}

/// <summary>
/// Request model for advanced order filtering.
/// </summary>
public record FilterOrdersRequest
{
    public OrderFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public OrderSortBy SortBy { get; init; } = OrderSortBy.OrderDate;
    public bool SortDescending { get; init; } = true;
}

#endregion
