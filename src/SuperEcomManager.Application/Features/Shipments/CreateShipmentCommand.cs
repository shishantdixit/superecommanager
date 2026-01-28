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
    public Guid? CourierAccountId { get; init; }
    public CourierType? CourierType { get; init; }
    public AddressDto? PickupAddress { get; init; }
    public DimensionsDto? Dimensions { get; init; }
    public string? ServiceCode { get; init; }
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
    private readonly ICourierService _courierService;
    private readonly ILogger<CreateShipmentCommandHandler> _logger;

    public CreateShipmentCommandHandler(
        ITenantDbContext dbContext,
        ICourierService courierService,
        ILogger<CreateShipmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _courierService = courierService;
        _logger = logger;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(
        CreateShipmentCommand request,
        CancellationToken cancellationToken)
    {
        // Determine courier type and pickup location
        CourierType courierType;
        string pickupName = "Default Warehouse"; // Default value

        if (request.CourierAccountId.HasValue)
        {
            var courierAccount = await _dbContext.CourierAccounts
                .FirstOrDefaultAsync(c => c.Id == request.CourierAccountId.Value && c.DeletedAt == null, cancellationToken);

            if (courierAccount == null)
            {
                return Result<ShipmentDetailDto>.Failure("Courier account not found");
            }

            if (!courierAccount.IsActive)
            {
                return Result<ShipmentDetailDto>.Failure("Courier account is not active");
            }

            if (!courierAccount.IsConnected)
            {
                return Result<ShipmentDetailDto>.Failure("Courier account is not connected");
            }

            courierType = courierAccount.CourierType;

            // Extract pickup location name from settings JSON if available
            if (!string.IsNullOrWhiteSpace(courierAccount.SettingsJson))
            {
                try
                {
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(courierAccount.SettingsJson);
                    if (settings != null &&
                        settings.TryGetValue("pickupLocation", out var pickupLocationValue) &&
                        !string.IsNullOrWhiteSpace(pickupLocationValue?.ToString()))
                    {
                        pickupName = pickupLocationValue.ToString()!;
                    }
                }
                catch
                {
                    // If JSON parsing fails, use default pickupName
                }
            }
        }
        else if (request.CourierType.HasValue)
        {
            courierType = request.CourierType.Value;
        }
        else
        {
            return Result<ShipmentDetailDto>.Failure("Either CourierAccountId or CourierType must be provided");
        }

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
            : new Address(
                pickupName, // Use pickup name from courier settings
                "0000000000", // TODO: Get from tenant settings
                "Warehouse Address Line 1", // TODO: Get from tenant settings
                null,
                "Mumbai", // TODO: Get from tenant settings
                "Maharashtra", // TODO: Get from tenant settings
                "400001", // TODO: Get from tenant settings
                "India");

        // Create new delivery address instance (can't reuse owned entities)
        var deliveryAddress = new Address(
            !string.IsNullOrWhiteSpace(order.ShippingAddress.Name) ? order.ShippingAddress.Name : order.CustomerName,
            order.ShippingAddress.Phone ?? order.CustomerPhone,
            order.ShippingAddress.Line1,
            order.ShippingAddress.Line2,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.PostalCode,
            order.ShippingAddress.Country);

        // Create new COD amount instance if needed (can't reuse owned entities)
        var codAmount = order.IsCOD && order.TotalAmount != null
            ? new Money(order.TotalAmount.Amount, order.TotalAmount.Currency)
            : null;

        // Create shipment
        var shipment = Shipment.Create(
            order.Id,
            pickupAddress,
            deliveryAddress,
            courierType,
            order.IsCOD,
            codAmount);

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

        // Step 1: Call Shiprocket API FIRST (before saving to database)
        _logger.LogInformation(
            "Calling {CourierType} API to create shipment for order {OrderNumber}",
            courierType, order.OrderNumber);

        var courierResult = await _courierService.CreateShipmentAsync(
            shipment,
            order,
            request.CourierAccountId,
            request.ServiceCode,
            cancellationToken);

        // Check for complete failure (order was NOT created in courier system)
        if (!courierResult.Success && string.IsNullOrEmpty(courierResult.ExternalOrderId))
        {
            // Courier API failed completely - do NOT save shipment to database
            _logger.LogError(
                "Failed to create shipment in courier system for order {OrderId}: {Error}",
                order.Id, courierResult.ErrorMessage);

            return Result<ShipmentDetailDto>.Failure(
                courierResult.ErrorMessage ?? "Failed to create shipment with courier");
        }

        // Step 2: Set external references (order was created in courier system)
        if (!string.IsNullOrEmpty(courierResult.ExternalOrderId))
        {
            shipment.SetExternalReferences(
                courierResult.ExternalOrderId,
                courierResult.ExternalShipmentId);
        }

        // Step 3: Set AWB if assigned (partial success means no AWB)
        if (!string.IsNullOrEmpty(courierResult.AwbNumber))
        {
            shipment.SetAwb(
                courierResult.AwbNumber,
                courierResult.CourierName,
                courierResult.LabelUrl,
                courierResult.TrackingUrl);
        }
        else if (courierResult.IsPartialSuccess)
        {
            _logger.LogWarning(
                "Shipment created in courier system but AWB not assigned for order {OrderId}: {Error}",
                order.Id, courierResult.ErrorMessage);
        }

        // Step 4: Save shipment to database
        try
        {
            _dbContext.Shipments.Add(shipment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (courierResult.IsPartialSuccess)
            {
                _logger.LogInformation(
                    "Shipment {ShipmentNumber} created successfully (awaiting courier assignment). External Order ID: {ExternalOrderId}",
                    shipment.ShipmentNumber, courierResult.ExternalOrderId);
            }
            else
            {
                _logger.LogInformation(
                    "Shipment {ShipmentNumber} created successfully with AWB {AWB}",
                    shipment.ShipmentNumber, courierResult.AwbNumber);
            }
        }
        catch (Exception ex)
        {
            var awbInfo = !string.IsNullOrEmpty(courierResult.AwbNumber)
                ? $"AWB: {courierResult.AwbNumber}"
                : $"External Order ID: {courierResult.ExternalOrderId}";

            _logger.LogError(ex,
                "Error saving shipment to database for order {OrderId}. Shipment was created in courier system ({AwbInfo})",
                order.Id, awbInfo);

            return Result<ShipmentDetailDto>.Failure(
                $"Shipment was created in courier system ({awbInfo}) but failed to save to database: {ex.Message}");
        }

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
            ExternalOrderId = shipment.ExternalOrderId,
            ExternalShipmentId = shipment.ExternalShipmentId,
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

        // If partial success, include warning message
        if (courierResult.IsPartialSuccess)
        {
            return Result<ShipmentDetailDto>.SuccessWithWarning(
                dto,
                $"Shipment created but courier not assigned: {courierResult.ErrorMessage}. You can assign a courier later.");
        }

        return Result<ShipmentDetailDto>.Success(dto);
    }
}
