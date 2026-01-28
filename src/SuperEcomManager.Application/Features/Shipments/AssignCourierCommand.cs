using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Command to assign a courier to a shipment that was created without AWB.
/// </summary>
[RequirePermission("shipments.update")]
[RequireFeature("shipping_management")]
public record AssignCourierCommand : IRequest<Result<ShipmentDetailDto>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
    public int? CourierId { get; init; }
}

public class AssignCourierCommandHandler : IRequestHandler<AssignCourierCommand, Result<ShipmentDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShiprocketChannelService _shiprocketService;
    private readonly ILogger<AssignCourierCommandHandler> _logger;

    public AssignCourierCommandHandler(
        ITenantDbContext dbContext,
        IShiprocketChannelService shiprocketService,
        ILogger<AssignCourierCommandHandler> logger)
    {
        _dbContext = dbContext;
        _shiprocketService = shiprocketService;
        _logger = logger;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(
        AssignCourierCommand request,
        CancellationToken cancellationToken)
    {
        // Get the shipment with items
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<ShipmentDetailDto>.Failure("Shipment not found");
        }

        // Verify shipment is in correct state
        if (shipment.Status != ShipmentStatus.Created)
        {
            return Result<ShipmentDetailDto>.Failure(
                $"Cannot assign courier to shipment in '{shipment.Status}' status. Only 'Created' shipments can be assigned.");
        }

        if (string.IsNullOrEmpty(shipment.ExternalShipmentId))
        {
            return Result<ShipmentDetailDto>.Failure(
                "Shipment does not have an external shipment ID. The order may not have been created in the courier system.");
        }

        if (!string.IsNullOrEmpty(shipment.AwbNumber))
        {
            return Result<ShipmentDetailDto>.Failure(
                $"Shipment already has AWB assigned: {shipment.AwbNumber}");
        }

        // Get courier account
        var courierAccount = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(ca => ca.CourierType == shipment.CourierType &&
                                       ca.IsActive &&
                                       ca.IsConnected &&
                                       ca.DeletedAt == null, cancellationToken);

        if (courierAccount == null)
        {
            return Result<ShipmentDetailDto>.Failure(
                $"No active {shipment.CourierType} courier account configured");
        }

        // Parse external shipment ID
        if (!long.TryParse(shipment.ExternalShipmentId, out var externalShipmentId))
        {
            return Result<ShipmentDetailDto>.Failure(
                $"Invalid external shipment ID format: {shipment.ExternalShipmentId}");
        }

        _logger.LogInformation(
            "Assigning courier to shipment {ShipmentId}. External shipment ID: {ExternalShipmentId}, Courier ID: {CourierId}",
            shipment.Id, externalShipmentId, request.CourierId?.ToString() ?? "auto");

        // Generate AWB
        var awbResult = await _shiprocketService.GenerateAwbAsync(
            courierAccount.Id,
            externalShipmentId,
            request.CourierId,
            cancellationToken);

        if (!awbResult.Success)
        {
            _logger.LogWarning(
                "Failed to assign courier to shipment {ShipmentId}: {Error}",
                shipment.Id, awbResult.ErrorMessage);

            return Result<ShipmentDetailDto>.Failure(
                awbResult.ErrorMessage ?? "Failed to assign courier");
        }

        // Update shipment with AWB details
        shipment.SetAwb(
            awbResult.AwbCode!,
            awbResult.CourierName,
            awbResult.LabelUrl,
            awbResult.TrackingUrl);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Courier assigned successfully to shipment {ShipmentId}. AWB: {AwbCode}, Courier: {CourierName}",
            shipment.Id, awbResult.AwbCode, awbResult.CourierName);

        // Get order info for response
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        // Build response DTO
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
