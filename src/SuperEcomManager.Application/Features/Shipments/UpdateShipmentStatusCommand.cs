using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Command to update the status of a shipment.
/// </summary>
[RequirePermission("shipments.edit")]
[RequireFeature("shipping_management")]
public record UpdateShipmentStatusCommand : IRequest<Result<ShipmentDetailDto>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
    public ShipmentStatus NewStatus { get; init; }
    public string? Location { get; init; }
    public string? Remarks { get; init; }
}

public class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, Result<ShipmentDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateShipmentStatusCommandHandler> _logger;

    public UpdateShipmentStatusCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateShipmentStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(
        UpdateShipmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<ShipmentDetailDto>.Failure("Shipment not found");
        }

        var oldStatus = shipment.Status;

        // Validate status transition
        if (!IsValidStatusTransition(oldStatus, request.NewStatus))
        {
            return Result<ShipmentDetailDto>.Failure(
                $"Invalid status transition from {oldStatus} to {request.NewStatus}");
        }

        shipment.UpdateStatus(request.NewStatus, request.Location, request.Remarks);

        // Update order status based on shipment status
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId && o.DeletedAt == null, cancellationToken);

        if (order != null)
        {
            UpdateOrderStatusFromShipment(order, request.NewStatus);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shipment {ShipmentId} status changed from {OldStatus} to {NewStatus}",
            shipment.Id, oldStatus, request.NewStatus);

        var dto = MapToDetailDto(shipment, order);
        return Result<ShipmentDetailDto>.Success(dto);
    }

    private static bool IsValidStatusTransition(ShipmentStatus current, ShipmentStatus next)
    {
        return (current, next) switch
        {
            (ShipmentStatus.Created, ShipmentStatus.Manifested) => true,
            (ShipmentStatus.Created, ShipmentStatus.Cancelled) => true,
            (ShipmentStatus.Manifested, ShipmentStatus.PickedUp) => true,
            (ShipmentStatus.Manifested, ShipmentStatus.Cancelled) => true,
            (ShipmentStatus.PickedUp, ShipmentStatus.InTransit) => true,
            (ShipmentStatus.InTransit, ShipmentStatus.ReachedDestination) => true,
            (ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery) => true,
            (ShipmentStatus.ReachedDestination, ShipmentStatus.OutForDelivery) => true,
            (ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered) => true,
            (ShipmentStatus.OutForDelivery, ShipmentStatus.DeliveryFailed) => true,
            (ShipmentStatus.DeliveryFailed, ShipmentStatus.OutForDelivery) => true,
            (ShipmentStatus.DeliveryFailed, ShipmentStatus.RTOInitiated) => true,
            (ShipmentStatus.RTOInitiated, ShipmentStatus.RTOInTransit) => true,
            (ShipmentStatus.RTOInTransit, ShipmentStatus.RTODelivered) => true,
            (_, ShipmentStatus.Lost) => true,
            _ => false
        };
    }

    private static void UpdateOrderStatusFromShipment(Domain.Entities.Orders.Order order, ShipmentStatus shipmentStatus)
    {
        switch (shipmentStatus)
        {
            case ShipmentStatus.PickedUp:
            case ShipmentStatus.InTransit:
            case ShipmentStatus.ReachedDestination:
            case ShipmentStatus.OutForDelivery:
                if (order.Status != OrderStatus.Shipped)
                    order.UpdateStatus(OrderStatus.Shipped, null, "Shipment in transit");
                break;
            case ShipmentStatus.Delivered:
                order.UpdateStatus(OrderStatus.Delivered, null, "Shipment delivered");
                break;
            case ShipmentStatus.RTOInitiated:
            case ShipmentStatus.RTOInTransit:
            case ShipmentStatus.RTODelivered:
                order.UpdateStatus(OrderStatus.RTO, null, "Shipment RTO");
                break;
        }
    }

    private static ShipmentDetailDto MapToDetailDto(
        Domain.Entities.Shipments.Shipment shipment,
        Domain.Entities.Orders.Order? order)
    {
        return new ShipmentDetailDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            OrderNumber = order?.OrderNumber ?? "Unknown",
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
            Dimensions = shipment.Dimensions != null ? new DimensionsDto
            {
                Length = shipment.Dimensions.LengthCm,
                Width = shipment.Dimensions.WidthCm,
                Height = shipment.Dimensions.HeightCm,
                Weight = shipment.Dimensions.WeightKg
            } : null,
            ShippingCost = shipment.ShippingCost?.Amount,
            ShippingCostCurrency = shipment.ShippingCost?.Currency,
            CODAmount = shipment.CODAmount?.Amount,
            CODCurrency = shipment.CODAmount?.Currency,
            IsCOD = shipment.IsCOD,
            PickedUpAt = shipment.PickedUpAt,
            DeliveredAt = shipment.DeliveredAt,
            ExpectedDeliveryDate = shipment.ExpectedDeliveryDate,
            LabelUrl = shipment.LabelUrl,
            TrackingUrl = shipment.TrackingUrl,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt,
            CustomerName = order?.CustomerName ?? "Unknown",
            CustomerPhone = order?.CustomerPhone,
            Items = shipment.Items.Select(i => new ShipmentItemDto
            {
                Id = i.Id,
                OrderItemId = i.OrderItemId,
                Sku = i.Sku,
                Name = i.Name,
                Quantity = i.Quantity
            }).ToList(),
            TrackingHistory = shipment.TrackingEvents
                .OrderByDescending(t => t.EventTime)
                .Select(t => new ShipmentTrackingDto
                {
                    Id = t.Id,
                    Status = t.Status,
                    Location = t.Location,
                    Remarks = t.Remarks,
                    EventTime = t.EventTime
                }).ToList()
        };
    }
}
