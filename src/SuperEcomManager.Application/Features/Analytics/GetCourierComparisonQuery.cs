using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Query to get courier comparison analytics.
/// </summary>
[RequirePermission("analytics.view")]
[RequireFeature("analytics")]
public record GetCourierComparisonQuery : IRequest<Result<CourierComparisonDto>>, ITenantRequest
{
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Last30Days;
    public DateTime? CustomStartDate { get; init; }
    public DateTime? CustomEndDate { get; init; }
}

public class GetCourierComparisonQueryHandler : IRequestHandler<GetCourierComparisonQuery, Result<CourierComparisonDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetCourierComparisonQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CourierComparisonDto>> Handle(
        GetCourierComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, _, _) = AnalyticsPeriodHelper.GetDateRange(
            request.Period,
            request.CustomStartDate,
            request.CustomEndDate);

        // Get shipments with NDR count
        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .Select(s => new
            {
                s.Id,
                s.CourierType,
                s.CourierName,
                s.Status,
                s.PickedUpAt,
                s.DeliveredAt,
                ShippingCost = s.ShippingCost != null ? s.ShippingCost.Amount : 0
            })
            .ToListAsync(cancellationToken);

        // Get NDR counts by shipment
        var shipmentIds = shipments.Select(s => s.Id).ToList();
        var ndrCounts = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => shipmentIds.Contains(n.ShipmentId))
            .GroupBy(n => n.ShipmentId)
            .Select(g => new { ShipmentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var ndrByShipment = ndrCounts.ToDictionary(n => n.ShipmentId, n => n.Count);

        // Get courier accounts for names
        var courierAccounts = await _dbContext.CourierAccounts
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.CourierType })
            .ToListAsync(cancellationToken);

        var rtoStatuses = new[] { ShipmentStatus.RTOInitiated, ShipmentStatus.RTOInTransit, ShipmentStatus.RTODelivered };

        // Group by courier type
        var courierPerformances = shipments
            .GroupBy(s => s.CourierType)
            .Select(g =>
            {
                var courierAccount = courierAccounts.FirstOrDefault(c => c.CourierType == g.Key);
                var totalShipments = g.Count();
                var deliveredCount = g.Count(s => s.Status == ShipmentStatus.Delivered);
                var rtoCount = g.Count(s => rtoStatuses.Contains(s.Status));
                var ndrCount = g.Sum(s => ndrByShipment.GetValueOrDefault(s.Id, 0));

                var deliveredWithDates = g
                    .Where(s => s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue && s.PickedUpAt.HasValue)
                    .Select(s => (s.DeliveredAt!.Value - s.PickedUpAt!.Value).TotalDays)
                    .ToList();

                var avgDeliveryDays = deliveredWithDates.Any() ? (decimal)deliveredWithDates.Average() : 0;
                var totalCost = g.Sum(s => s.ShippingCost);
                var avgCost = totalShipments > 0 ? totalCost / totalShipments : 0;

                return new CourierPerformanceDto
                {
                    CourierId = courierAccount?.Id ?? Guid.Empty,
                    CourierName = courierAccount?.Name ?? g.Key.ToString(),
                    CourierType = g.Key.ToString(),
                    TotalShipments = totalShipments,
                    DeliveredCount = deliveredCount,
                    RtoCount = rtoCount,
                    NdrCount = ndrCount,
                    DeliveryRate = totalShipments > 0 ? Math.Round(((decimal)deliveredCount / totalShipments) * 100, 2) : 0,
                    RtoRate = totalShipments > 0 ? Math.Round(((decimal)rtoCount / totalShipments) * 100, 2) : 0,
                    NdrRate = totalShipments > 0 ? Math.Round(((decimal)ndrCount / totalShipments) * 100, 2) : 0,
                    AverageDeliveryDays = Math.Round(avgDeliveryDays, 1),
                    AverageCost = Math.Round(avgCost, 2),
                    TotalCost = Math.Round(totalCost, 2)
                };
            })
            .OrderByDescending(c => c.TotalShipments)
            .ToList();

        // Find best performers
        var bestDeliveryRate = courierPerformances
            .Where(c => c.TotalShipments >= 10) // Minimum threshold
            .OrderByDescending(c => c.DeliveryRate)
            .FirstOrDefault();

        var fastestDelivery = courierPerformances
            .Where(c => c.TotalShipments >= 10 && c.AverageDeliveryDays > 0)
            .OrderBy(c => c.AverageDeliveryDays)
            .FirstOrDefault();

        var lowestRto = courierPerformances
            .Where(c => c.TotalShipments >= 10)
            .OrderBy(c => c.RtoRate)
            .FirstOrDefault();

        var result = new CourierComparisonDto
        {
            Couriers = courierPerformances,
            BestDeliveryRateCourierId = bestDeliveryRate?.CourierId,
            BestDeliveryRateCourierName = bestDeliveryRate?.CourierName,
            FastestDeliveryCourierId = fastestDelivery?.CourierId,
            FastestDeliveryCourierName = fastestDelivery?.CourierName,
            LowestRtoCourierId = lowestRto?.CourierId,
            LowestRtoCourierName = lowestRto?.CourierName
        };

        return Result<CourierComparisonDto>.Success(result);
    }
}
