using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Shipments;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Bulk;

/// <summary>
/// Command to create shipments for multiple orders at once.
/// </summary>
[RequirePermission("shipments.bulk")]
[RequireFeature("shipments")]
public record BulkCreateShipmentsCommand : IRequest<Result<BulkOperationResultDto>>, ITenantRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public CourierType CourierType { get; init; }
    public Guid? CourierAccountId { get; init; }
}

public class BulkCreateShipmentsCommandHandler : IRequestHandler<BulkCreateShipmentsCommand, Result<BulkOperationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxBatchSize = 50;

    public BulkCreateShipmentsCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkCreateShipmentsCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!request.OrderIds.Any())
        {
            return Result<BulkOperationResultDto>.Failure("No order IDs provided.");
        }

        if (request.OrderIds.Count > MaxBatchSize)
        {
            return Result<BulkOperationResultDto>.Failure($"Maximum {MaxBatchSize} shipments can be created at once.");
        }

        var errors = new List<BulkOperationErrorDto>();
        var successfulIds = new List<Guid>();

        // Get all orders with their items
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var foundIds = orders.Select(o => o.Id).ToHashSet();
        var notFoundIds = request.OrderIds.Except(foundIds).ToList();

        foreach (var notFoundId in notFoundIds)
        {
            errors.Add(new BulkOperationErrorDto
            {
                ItemId = notFoundId,
                Error = "Order not found"
            });
        }

        // Check for existing shipments
        var existingShipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => request.OrderIds.Contains(s.OrderId) && s.Status != ShipmentStatus.Cancelled)
            .Select(s => s.OrderId)
            .ToListAsync(cancellationToken);

        var existingShipmentOrderIds = existingShipments.ToHashSet();

        // Get pickup address from settings or courier account
        var courierAccount = request.CourierAccountId.HasValue
            ? await _dbContext.CourierAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CourierAccountId.Value, cancellationToken)
            : await _dbContext.CourierAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourierType == request.CourierType && c.IsActive && c.IsDefault, cancellationToken);

        if (courierAccount == null)
        {
            return Result<BulkOperationResultDto>.Failure("No active courier account found for the specified courier type.");
        }

        // Get tenant settings for pickup address
        var settings = await _dbContext.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var order in orders)
        {
            try
            {
                // Skip if already has active shipment
                if (existingShipmentOrderIds.Contains(order.Id))
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = order.Id,
                        Reference = order.OrderNumber,
                        Error = "Order already has an active shipment"
                    });
                    continue;
                }

                // Validate order status
                if (order.Status == OrderStatus.Cancelled)
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = order.Id,
                        Reference = order.OrderNumber,
                        Error = "Cannot create shipment for cancelled order"
                    });
                    continue;
                }

                if (order.Status == OrderStatus.Delivered)
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = order.Id,
                        Reference = order.OrderNumber,
                        Error = "Order is already delivered"
                    });
                    continue;
                }

                // Create shipment
                var shipment = Shipment.Create(
                    order.Id,
                    order.ShippingAddress, // Use shipping address as pickup (will be updated)
                    order.ShippingAddress,
                    request.CourierType,
                    order.IsCOD,
                    order.IsCOD ? order.TotalAmount : null);

                // Add shipment items from order items
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

                _dbContext.Shipments.Add(shipment);
                successfulIds.Add(shipment.Id);

                // Update order status to Processing if Confirmed
                if (order.Status == OrderStatus.Confirmed)
                {
                    order.UpdateStatus(OrderStatus.Processing);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto
                {
                    ItemId = order.Id,
                    Reference = order.OrderNumber,
                    Error = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var result = new BulkOperationResultDto
        {
            TotalRequested = request.OrderIds.Count,
            SuccessCount = successfulIds.Count,
            FailedCount = errors.Count,
            Errors = errors,
            SuccessfulIds = successfulIds,
            Duration = stopwatch.Elapsed
        };

        return Result<BulkOperationResultDto>.Success(result);
    }
}
