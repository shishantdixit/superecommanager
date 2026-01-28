using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;
using System.Text.Json;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Query to get available couriers for a shipment based on serviceability.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record GetAvailableCouriersQuery : IRequest<Result<List<AvailableCourierDto>>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
}

/// <summary>
/// DTO for available courier options.
/// </summary>
public record AvailableCourierDto
{
    public int CourierId { get; init; }
    public string CourierName { get; init; } = string.Empty;
    public decimal FreightCharge { get; init; }
    public decimal CodCharges { get; init; }
    public decimal TotalCharge { get; init; }
    public string EstimatedDeliveryDays { get; init; } = string.Empty;
    public string? Etd { get; init; }
    public decimal Rating { get; init; }
    public bool IsSurface { get; init; }
    public bool IsRecommended { get; init; }
}

public class GetAvailableCouriersQueryHandler : IRequestHandler<GetAvailableCouriersQuery, Result<List<AvailableCourierDto>>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShiprocketChannelService _shiprocketService;
    private readonly ILogger<GetAvailableCouriersQueryHandler> _logger;

    public GetAvailableCouriersQueryHandler(
        ITenantDbContext dbContext,
        IShiprocketChannelService shiprocketService,
        ILogger<GetAvailableCouriersQueryHandler> logger)
    {
        _dbContext = dbContext;
        _shiprocketService = shiprocketService;
        _logger = logger;
    }

    public async Task<Result<List<AvailableCourierDto>>> Handle(
        GetAvailableCouriersQuery request,
        CancellationToken cancellationToken)
    {
        // Get the shipment
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<List<AvailableCourierDto>>.Failure("Shipment not found");
        }

        // Verify shipment can have courier assigned
        if (shipment.Status != ShipmentStatus.Created)
        {
            return Result<List<AvailableCourierDto>>.Failure(
                $"Cannot assign courier to shipment in '{shipment.Status}' status. Only 'Created' shipments can be assigned.");
        }

        if (string.IsNullOrEmpty(shipment.ExternalShipmentId))
        {
            return Result<List<AvailableCourierDto>>.Failure(
                "Shipment does not have an external reference. Create shipment in courier system first.");
        }

        // Get order for COD info
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<List<AvailableCourierDto>>.Failure("Associated order not found");
        }

        // Get courier account for credentials
        var courierAccount = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(ca => ca.CourierType == shipment.CourierType &&
                                       ca.IsActive &&
                                       ca.IsConnected &&
                                       ca.DeletedAt == null, cancellationToken);

        if (courierAccount == null)
        {
            return Result<List<AvailableCourierDto>>.Failure(
                $"No active {shipment.CourierType} courier account configured");
        }

        // Get weight from shipment dimensions or use default
        var weight = shipment.Dimensions?.WeightKg ?? 0.5m;

        _logger.LogInformation(
            "Checking courier serviceability for shipment {ShipmentId}: pickup {PickupPincode} → delivery {DeliveryPincode}, weight {Weight}kg, COD {IsCOD}",
            shipment.Id,
            shipment.PickupAddress.PostalCode,
            shipment.DeliveryAddress.PostalCode,
            weight,
            order.IsCOD);

        // Call Shiprocket serviceability API
        var serviceabilityResult = await _shiprocketService.CheckServiceabilityAsync(
            courierAccount.Id,
            shipment.PickupAddress.PostalCode,
            shipment.DeliveryAddress.PostalCode,
            weight,
            order.IsCOD,
            long.TryParse(shipment.ExternalOrderId, out var orderId) ? orderId : null,
            cancellationToken);

        if (!serviceabilityResult.Success)
        {
            _logger.LogWarning(
                "Serviceability check failed for shipment {ShipmentId}: {Error}",
                shipment.Id, serviceabilityResult.ErrorMessage);

            return Result<List<AvailableCourierDto>>.Failure(
                serviceabilityResult.ErrorMessage ?? "Failed to check courier serviceability");
        }

        if (serviceabilityResult.AvailableCouriers == null || !serviceabilityResult.AvailableCouriers.Any())
        {
            return Result<List<AvailableCourierDto>>.Failure(
                "No couriers available for this route. The delivery pincode may not be serviceable.");
        }

        // Map to DTOs
        var couriers = serviceabilityResult.AvailableCouriers
            .Select(c => new AvailableCourierDto
            {
                CourierId = c.CourierId,
                CourierName = c.CourierName,
                FreightCharge = c.FreightCharge,
                CodCharges = c.CodCharges,
                TotalCharge = c.FreightCharge + c.CodCharges,
                EstimatedDeliveryDays = c.EstimatedDeliveryDays ?? "N/A",
                Etd = c.Etd,
                Rating = c.Rating,
                IsSurface = c.IsSurface,
                IsRecommended = c.CourierId == serviceabilityResult.RecommendedCourierId
            })
            .OrderBy(c => !c.IsRecommended) // Recommended first
            .ThenBy(c => c.TotalCharge) // Then by price
            .ToList();

        _logger.LogInformation(
            "Found {Count} available couriers for shipment {ShipmentId}. Recommended: {RecommendedId}",
            couriers.Count, shipment.Id, serviceabilityResult.RecommendedCourierId);

        return Result<List<AvailableCourierDto>>.Success(couriers);
    }
}
