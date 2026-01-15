using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Shipments;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Command to create a shipment for an order.
/// </summary>
[RequirePermission("shipments.create")]
[RequireFeature("shipping_management")]
public record CreateShipmentCommand : IRequest<Result<ShipmentDetailDto>>, ITenantRequest
{
    public Guid OrderId { get; init; }
    public CourierType CourierType { get; init; }
    public AddressDto? PickupAddress { get; init; }
    public DimensionsDto? Dimensions { get; init; }
    public List<CreateShipmentItemDto>? Items { get; init; }
}

/// <summary>
/// DTO for creating shipment items.
/// </summary>
public record CreateShipmentItemDto
{
    public Guid OrderItemId { get; init; }
    public int Quantity { get; init; }
}

public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Result<ShipmentDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<CreateShipmentCommandHandler> _logger;

    public CreateShipmentCommandHandler(
        ITenantDbContext dbContext,
        ILogger<CreateShipmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(
        CreateShipmentCommand request,
        CancellationToken cancellationToken)
    {
        // Get the order
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.DeletedAt == null, cancellationToken);

        if (order == null)
        {
            return Result<ShipmentDetailDto>.Failure("Order not found");
        }

        // Check if order can have shipment created
        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
        {
            return Result<ShipmentDetailDto>.Failure($"Cannot create shipment for order in {order.Status} status");
        }

        // Check for existing active shipment
        var existingShipment = await _dbContext.Shipments
            .AnyAsync(s => s.OrderId == request.OrderId &&
                          s.DeletedAt == null &&
                          s.Status != ShipmentStatus.Cancelled &&
                          s.Status != ShipmentStatus.RTODelivered,
                cancellationToken);

        if (existingShipment)
        {
            return Result<ShipmentDetailDto>.Failure("Order already has an active shipment");
        }

        // Use default pickup address if not provided (from tenant settings in future)
        var pickupAddress = request.PickupAddress != null
            ? new Address(
                request.PickupAddress.Name,
                request.PickupAddress.Phone,
                request.PickupAddress.Line1,
                request.PickupAddress.Line2,
                request.PickupAddress.City,
                request.PickupAddress.State,
                request.PickupAddress.PostalCode,
                request.PickupAddress.Country)
            : order.ShippingAddress; // Use order shipping address as fallback

        // Create shipment
        var shipment = Shipment.Create(
            order.Id,
            pickupAddress,
            order.ShippingAddress,
            request.CourierType,
            order.IsCOD,
            order.IsCOD ? order.TotalAmount : null);

        // Set dimensions if provided
        if (request.Dimensions != null)
        {
            // Dimensions would need to be added to the shipment
            // For now, we'll store this in the future
        }

        // Add items
        if (request.Items != null && request.Items.Count > 0)
        {
            foreach (var itemDto in request.Items)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == itemDto.OrderItemId);
                if (orderItem != null)
                {
                    var shipmentItem = new ShipmentItem(
                        shipment.Id,
                        orderItem.Id,
                        orderItem.Sku,
                        orderItem.Name,
                        itemDto.Quantity);
                    shipment.AddItem(shipmentItem);
                }
            }
        }
        else
        {
            // Add all order items by default
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
        }

        _dbContext.Shipments.Add(shipment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shipment {ShipmentNumber} created for order {OrderId} with courier {CourierType}",
            shipment.ShipmentNumber, order.Id, request.CourierType);

        // Return the created shipment
        var dto = new ShipmentDetailDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            OrderNumber = order.OrderNumber,
            ShipmentNumber = shipment.ShipmentNumber,
            AwbNumber = shipment.AwbNumber,
            CourierType = shipment.CourierType,
            CourierName = shipment.CourierName,
            Status = shipment.Status,
            PickupAddress = new AddressDto
            {
                Name = shipment.PickupAddress.Name,
                Line1 = shipment.PickupAddress.Line1,
                Line2 = shipment.PickupAddress.Line2,
                City = shipment.PickupAddress.City,
                State = shipment.PickupAddress.State,
                PostalCode = shipment.PickupAddress.PostalCode,
                Country = shipment.PickupAddress.Country,
                Phone = shipment.PickupAddress.Phone
            },
            DeliveryAddress = new AddressDto
            {
                Name = shipment.DeliveryAddress.Name,
                Line1 = shipment.DeliveryAddress.Line1,
                Line2 = shipment.DeliveryAddress.Line2,
                City = shipment.DeliveryAddress.City,
                State = shipment.DeliveryAddress.State,
                PostalCode = shipment.DeliveryAddress.PostalCode,
                Country = shipment.DeliveryAddress.Country,
                Phone = shipment.DeliveryAddress.Phone
            },
            IsCOD = shipment.IsCOD,
            CODAmount = shipment.CODAmount?.Amount,
            CODCurrency = shipment.CODAmount?.Currency,
            CreatedAt = shipment.CreatedAt,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            Items = shipment.Items.Select(i => new ShipmentItemDto
            {
                Id = i.Id,
                OrderItemId = i.OrderItemId,
                Sku = i.Sku,
                Name = i.Name,
                Quantity = i.Quantity
            }).ToList(),
            TrackingHistory = new List<ShipmentTrackingDto>()
        };

        return Result<ShipmentDetailDto>.Success(dto);
    }
}
