using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Query to get a single shipment by ID with full details.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record GetShipmentByIdQuery : IRequest<Result<ShipmentDetailDto>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
}

public class GetShipmentByIdQueryHandler : IRequestHandler<GetShipmentByIdQuery, Result<ShipmentDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetShipmentByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(
        GetShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.TrackingEvents)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<ShipmentDetailDto>.Failure("Shipment not found");
        }

        // Get order info separately
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        var dto = new ShipmentDetailDto
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
            ExternalOrderId = shipment.ExternalOrderId,
            ExternalShipmentId = shipment.ExternalShipmentId,
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

        return Result<ShipmentDetailDto>.Success(dto);
    }
}
