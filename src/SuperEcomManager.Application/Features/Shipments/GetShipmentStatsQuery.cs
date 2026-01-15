using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Query to get shipment statistics for dashboard.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record GetShipmentStatsQuery : IRequest<Result<ShipmentStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetShipmentStatsQueryHandler : IRequestHandler<GetShipmentStatsQuery, Result<ShipmentStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetShipmentStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ShipmentStatsDto>> Handle(
        GetShipmentStatsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var query = _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.DeletedAt == null &&
                        s.CreatedAt >= fromDate &&
                        s.CreatedAt <= toDate);

        var shipments = await query.ToListAsync(cancellationToken);

        // Status counts
        var statusCounts = shipments.GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // By courier
        var shipmentsByCourier = shipments
            .GroupBy(s => s.CourierName ?? s.CourierType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Delivery success rate
        var deliveredCount = statusCounts.GetValueOrDefault(ShipmentStatus.Delivered);
        var totalAttempted = deliveredCount +
                             statusCounts.GetValueOrDefault(ShipmentStatus.DeliveryFailed) +
                             statusCounts.GetValueOrDefault(ShipmentStatus.RTOInitiated) +
                             statusCounts.GetValueOrDefault(ShipmentStatus.RTOInTransit) +
                             statusCounts.GetValueOrDefault(ShipmentStatus.RTODelivered);
        var deliverySuccessRate = totalAttempted > 0 ? (decimal)deliveredCount / totalAttempted * 100 : 0;

        // Average delivery days
        var deliveredShipments = shipments
            .Where(s => s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue)
            .ToList();
        var avgDeliveryDays = deliveredShipments.Count > 0
            ? (decimal)deliveredShipments.Average(s => (s.DeliveredAt!.Value - s.CreatedAt).TotalDays)
            : 0;

        var stats = new ShipmentStatsDto
        {
            TotalShipments = shipments.Count,
            CreatedCount = statusCounts.GetValueOrDefault(ShipmentStatus.Created),
            ManifestedCount = statusCounts.GetValueOrDefault(ShipmentStatus.Manifested),
            PickedUpCount = statusCounts.GetValueOrDefault(ShipmentStatus.PickedUp),
            InTransitCount = statusCounts.GetValueOrDefault(ShipmentStatus.InTransit) +
                             statusCounts.GetValueOrDefault(ShipmentStatus.ReachedDestination),
            OutForDeliveryCount = statusCounts.GetValueOrDefault(ShipmentStatus.OutForDelivery),
            DeliveredCount = deliveredCount,
            DeliveryFailedCount = statusCounts.GetValueOrDefault(ShipmentStatus.DeliveryFailed),
            RTOCount = statusCounts.GetValueOrDefault(ShipmentStatus.RTOInitiated) +
                       statusCounts.GetValueOrDefault(ShipmentStatus.RTOInTransit) +
                       statusCounts.GetValueOrDefault(ShipmentStatus.RTODelivered),
            CancelledCount = statusCounts.GetValueOrDefault(ShipmentStatus.Cancelled),
            LostCount = statusCounts.GetValueOrDefault(ShipmentStatus.Lost),
            ShipmentsByCourier = shipmentsByCourier,
            DeliverySuccessRate = Math.Round(deliverySuccessRate, 2),
            AverageDeliveryDays = Math.Round(avgDeliveryDays, 1)
        };

        return Result<ShipmentStatsDto>.Success(stats);
    }
}
