using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Shipments;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.BulkOperations;

#region DTOs

/// <summary>
/// Generic bulk operation result.
/// </summary>
public record BulkOperationResult
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkOperationError> Errors { get; init; } = new();
}

/// <summary>
/// Error detail for bulk operation.
/// </summary>
public record BulkOperationError
{
    public string Identifier { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// DTO for bulk shipment creation input.
/// </summary>
public record BulkShipmentInput
{
    public Guid OrderId { get; init; }
    public CourierType CourierType { get; init; }
}

/// <summary>
/// Result item for bulk shipment creation.
/// </summary>
public record BulkShipmentResultItem
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid? ShipmentId { get; init; }
    public string? ShipmentNumber { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result for bulk shipment creation.
/// </summary>
public record BulkCreateShipmentsResult
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkShipmentResultItem> Results { get; init; } = new();
}

#endregion

#region Bulk Create Shipments

/// <summary>
/// Command to create shipments for multiple orders at once.
/// </summary>
[RequirePermission("shipments.create")]
[RequireFeature("shipping_management")]
public record BulkCreateShipmentsCommand : IRequest<Result<BulkCreateShipmentsResult>>, ITenantRequest
{
    public List<BulkShipmentInput> Orders { get; init; } = new();
    public CourierType? DefaultCourierType { get; init; }
}

public class BulkCreateShipmentsCommandHandler : IRequestHandler<BulkCreateShipmentsCommand, Result<BulkCreateShipmentsResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<BulkCreateShipmentsCommandHandler> _logger;
    private const int MaxBulkSize = 50;

    public BulkCreateShipmentsCommandHandler(
        ITenantDbContext dbContext,
        ILogger<BulkCreateShipmentsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<BulkCreateShipmentsResult>> Handle(
        BulkCreateShipmentsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Orders.Count == 0)
        {
            return Result<BulkCreateShipmentsResult>.Failure("No orders specified");
        }

        if (request.Orders.Count > MaxBulkSize)
        {
            return Result<BulkCreateShipmentsResult>.Failure($"Cannot create more than {MaxBulkSize} shipments at once");
        }

        var orderIds = request.Orders.Select(o => o.OrderId).Distinct().ToList();

        // Get orders with items
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => orderIds.Contains(o.Id) && o.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Get existing active shipments for these orders
        var ordersWithShipments = await _dbContext.Shipments
            .Where(s => orderIds.Contains(s.OrderId) &&
                        s.DeletedAt == null &&
                        s.Status != ShipmentStatus.Cancelled &&
                        s.Status != ShipmentStatus.RTODelivered)
            .Select(s => s.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var results = new List<BulkShipmentResultItem>();
        var shipmentsToAdd = new List<Shipment>();

        foreach (var input in request.Orders)
        {
            var order = orders.FirstOrDefault(o => o.Id == input.OrderId);

            if (order == null)
            {
                results.Add(new BulkShipmentResultItem
                {
                    OrderId = input.OrderId,
                    OrderNumber = "Unknown",
                    Success = false,
                    ErrorMessage = "Order not found"
                });
                continue;
            }

            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
            {
                results.Add(new BulkShipmentResultItem
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Success = false,
                    ErrorMessage = $"Cannot create shipment for order in {order.Status} status"
                });
                continue;
            }

            if (ordersWithShipments.Contains(order.Id))
            {
                results.Add(new BulkShipmentResultItem
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Success = false,
                    ErrorMessage = "Order already has an active shipment"
                });
                continue;
            }

            var courierType = input.CourierType != 0 ? input.CourierType : request.DefaultCourierType ?? CourierType.Shiprocket;

            var shipment = Shipment.Create(
                order.Id,
                order.ShippingAddress, // Use order address as pickup (can be changed later)
                order.ShippingAddress,
                courierType,
                order.IsCOD,
                order.IsCOD ? order.TotalAmount : null);

            // Add all order items
            foreach (var orderItem in order.Items)
            {
                var shipmentItem = new ShipmentItem(
                    shipment.Id,
                    orderItem.Id,
                    orderItem.Sku,
                    orderItem.Name,
                    orderItem.Quantity);
                shipment.AddItem(shipmentItem);
            }

            shipmentsToAdd.Add(shipment);
            ordersWithShipments.Add(order.Id); // Prevent duplicate in same batch

            results.Add(new BulkShipmentResultItem
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ShipmentId = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Success = true
            });
        }

        if (shipmentsToAdd.Count > 0)
        {
            _dbContext.Shipments.AddRange(shipmentsToAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk created {Count} shipments",
                shipmentsToAdd.Count);
        }

        return Result<BulkCreateShipmentsResult>.Success(new BulkCreateShipmentsResult
        {
            TotalRequested = request.Orders.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            Results = results
        });
    }
}

#endregion

#region Bulk Update Shipment Status

/// <summary>
/// Command to bulk update status of multiple shipments.
/// </summary>
[RequirePermission("shipments.edit")]
[RequireFeature("shipping_management")]
public record BulkUpdateShipmentStatusCommand : IRequest<Result<BulkOperationResult>>, ITenantRequest
{
    public List<Guid> ShipmentIds { get; init; } = new();
    public ShipmentStatus NewStatus { get; init; }
    public string? Remarks { get; init; }
}

public class BulkUpdateShipmentStatusCommandHandler : IRequestHandler<BulkUpdateShipmentStatusCommand, Result<BulkOperationResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BulkUpdateShipmentStatusCommandHandler> _logger;
    private const int MaxBulkSize = 100;

    public BulkUpdateShipmentStatusCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<BulkUpdateShipmentStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<BulkOperationResult>> Handle(
        BulkUpdateShipmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ShipmentIds.Count == 0)
        {
            return Result<BulkOperationResult>.Failure("No shipments specified");
        }

        if (request.ShipmentIds.Count > MaxBulkSize)
        {
            return Result<BulkOperationResult>.Failure($"Cannot update more than {MaxBulkSize} shipments at once");
        }

        var shipments = await _dbContext.Shipments
            .Where(s => request.ShipmentIds.Contains(s.Id) && s.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var shipment in shipments)
        {
            try
            {
                shipment.UpdateStatus(request.NewStatus, null, request.Remarks);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new BulkOperationError
                {
                    Identifier = shipment.ShipmentNumber,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Add errors for shipments not found
        var foundIds = shipments.Select(s => s.Id).ToHashSet();
        var notFoundIds = request.ShipmentIds.Where(id => !foundIds.Contains(id));
        foreach (var id in notFoundIds)
        {
            errors.Add(new BulkOperationError
            {
                Identifier = id.ToString(),
                ErrorMessage = "Shipment not found"
            });
        }

        if (successCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk updated {SuccessCount} shipments to status {NewStatus} by {UserId}",
                successCount, request.NewStatus, _currentUserService.UserId);
        }

        return Result<BulkOperationResult>.Success(new BulkOperationResult
        {
            TotalRequested = request.ShipmentIds.Count,
            SuccessCount = successCount,
            FailedCount = errors.Count,
            Errors = errors
        });
    }
}

#endregion

#region Bulk Cancel Shipments

/// <summary>
/// Command to bulk cancel shipments.
/// </summary>
[RequirePermission("shipments.cancel")]
[RequireFeature("shipping_management")]
public record BulkCancelShipmentsCommand : IRequest<Result<BulkOperationResult>>, ITenantRequest
{
    public List<Guid> ShipmentIds { get; init; } = new();
    public string? CancellationReason { get; init; }
}

public class BulkCancelShipmentsCommandHandler : IRequestHandler<BulkCancelShipmentsCommand, Result<BulkOperationResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<BulkCancelShipmentsCommandHandler> _logger;
    private const int MaxBulkSize = 50;

    public BulkCancelShipmentsCommandHandler(
        ITenantDbContext dbContext,
        ILogger<BulkCancelShipmentsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<BulkOperationResult>> Handle(
        BulkCancelShipmentsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ShipmentIds.Count == 0)
        {
            return Result<BulkOperationResult>.Failure("No shipments specified");
        }

        if (request.ShipmentIds.Count > MaxBulkSize)
        {
            return Result<BulkOperationResult>.Failure($"Cannot cancel more than {MaxBulkSize} shipments at once");
        }

        var shipments = await _dbContext.Shipments
            .Where(s => request.ShipmentIds.Contains(s.Id) && s.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var shipment in shipments)
        {
            try
            {
                shipment.UpdateStatus(ShipmentStatus.Cancelled, null, request.CancellationReason);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new BulkOperationError
                {
                    Identifier = shipment.ShipmentNumber,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Add errors for shipments not found
        var foundIds = shipments.Select(s => s.Id).ToHashSet();
        var notFoundIds = request.ShipmentIds.Where(id => !foundIds.Contains(id));
        foreach (var id in notFoundIds)
        {
            errors.Add(new BulkOperationError
            {
                Identifier = id.ToString(),
                ErrorMessage = "Shipment not found"
            });
        }

        if (successCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk cancelled {SuccessCount} shipments", successCount);
        }

        return Result<BulkOperationResult>.Success(new BulkOperationResult
        {
            TotalRequested = request.ShipmentIds.Count,
            SuccessCount = successCount,
            FailedCount = errors.Count,
            Errors = errors
        });
    }
}

#endregion
