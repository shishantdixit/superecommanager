using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Query to get delivery performance metrics.
/// </summary>
[RequirePermission("analytics.view")]
[RequireFeature("analytics")]
public record GetDeliveryPerformanceQuery : IRequest<Result<DeliveryPerformanceDto>>, ITenantRequest
{
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Last30Days;
    public DateTime? CustomStartDate { get; init; }
    public DateTime? CustomEndDate { get; init; }
}

public class GetDeliveryPerformanceQueryHandler : IRequestHandler<GetDeliveryPerformanceQuery, Result<DeliveryPerformanceDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetDeliveryPerformanceQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DeliveryPerformanceDto>> Handle(
        GetDeliveryPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, prevStartDate, prevEndDate) = AnalyticsPeriodHelper.GetDateRange(
            request.Period,
            request.CustomStartDate,
            request.CustomEndDate);

        // Current period shipments
        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .Select(s => new
            {
                s.Id,
                s.Status,
                s.CreatedAt,
                s.PickedUpAt,
                s.DeliveredAt,
                State = s.DeliveryAddress.State
            })
            .ToListAsync(cancellationToken);

        // Previous period for comparison
        var prevDeliveredShipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= prevStartDate && s.CreatedAt <= prevEndDate)
            .Where(s => s.Status == ShipmentStatus.Delivered && s.DeliveredAt != null && s.PickedUpAt != null)
            .Select(s => new { s.PickedUpAt, s.DeliveredAt })
            .ToListAsync(cancellationToken);

        var totalShipments = shipments.Count;
        var deliveredCount = shipments.Count(s => s.Status == ShipmentStatus.Delivered);
        var rtoStatuses = new[] { ShipmentStatus.RTOInitiated, ShipmentStatus.RTOInTransit, ShipmentStatus.RTODelivered };
        var rtoCount = shipments.Count(s => rtoStatuses.Contains(s.Status));
        var inTransitStatuses = new[] { ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery };
        var inTransitCount = shipments.Count(s => inTransitStatuses.Contains(s.Status));

        var deliveryRate = totalShipments > 0 ? ((decimal)deliveredCount / totalShipments) * 100 : 0;
        var rtoRate = totalShipments > 0 ? ((decimal)rtoCount / totalShipments) * 100 : 0;

        // Calculate average delivery days
        var deliveredWithDates = shipments
            .Where(s => s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue && s.PickedUpAt.HasValue)
            .Select(s => (s.DeliveredAt!.Value - s.PickedUpAt!.Value).TotalDays)
            .ToList();

        var avgDeliveryDays = deliveredWithDates.Any() ? (decimal)deliveredWithDates.Average() : 0;

        var prevDeliveryDays = prevDeliveredShipments
            .Where(s => s.DeliveredAt.HasValue && s.PickedUpAt.HasValue)
            .Select(s => (s.DeliveredAt!.Value - s.PickedUpAt!.Value).TotalDays)
            .ToList();

        var prevAvgDeliveryDays = prevDeliveryDays.Any() ? (decimal)prevDeliveryDays.Average() : 0;

        // Delivery time distribution
        var deliveryTimeDistribution = new List<DeliveryTimeDistributionDto>();
        if (deliveredWithDates.Any())
        {
            var ranges = new[] { (0, 2, "1-2 days"), (2, 5, "3-5 days"), (5, 7, "5-7 days"), (7, 14, "1-2 weeks"), (14, int.MaxValue, "2+ weeks") };
            foreach (var (min, max, label) in ranges)
            {
                var count = deliveredWithDates.Count(d => d >= min && d < max);
                deliveryTimeDistribution.Add(new DeliveryTimeDistributionDto
                {
                    Range = label,
                    Count = count,
                    Percentage = deliveredWithDates.Count > 0 ? ((decimal)count / deliveredWithDates.Count) * 100 : 0
                });
            }
        }

        // Daily deliveries
        var dailyDeliveries = shipments
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new DailyDeliveryDto
            {
                Date = g.Key,
                DeliveredCount = g.Count(s => s.Status == ShipmentStatus.Delivered),
                RtoCount = g.Count(s => rtoStatuses.Contains(s.Status)),
                NdrCount = g.Count(s => s.Status == ShipmentStatus.DeliveryFailed)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Delivery by state
        var deliveryByState = shipments
            .Where(s => !string.IsNullOrEmpty(s.State))
            .GroupBy(s => s.State)
            .Select(g =>
            {
                var stateDelivered = g.Where(s => s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue && s.PickedUpAt.HasValue).ToList();
                var stateAvgDays = stateDelivered.Any()
                    ? (decimal)stateDelivered.Average(s => (s.DeliveredAt!.Value - s.PickedUpAt!.Value).TotalDays)
                    : 0;

                return new StateDeliveryDto
                {
                    State = g.Key ?? "Unknown",
                    TotalShipments = g.Count(),
                    DeliveredCount = g.Count(s => s.Status == ShipmentStatus.Delivered),
                    RtoCount = g.Count(s => rtoStatuses.Contains(s.Status)),
                    DeliveryRate = g.Count() > 0 ? ((decimal)g.Count(s => s.Status == ShipmentStatus.Delivered) / g.Count()) * 100 : 0,
                    AverageDeliveryDays = Math.Round(stateAvgDays, 1)
                };
            })
            .OrderByDescending(s => s.TotalShipments)
            .Take(15)
            .ToList();

        var result = new DeliveryPerformanceDto
        {
            TotalShipments = totalShipments,
            DeliveredCount = deliveredCount,
            RtoCount = rtoCount,
            InTransitCount = inTransitCount,
            DeliveryRate = Math.Round(deliveryRate, 2),
            RtoRate = Math.Round(rtoRate, 2),
            AverageDeliveryDays = Math.Round(avgDeliveryDays, 1),
            PreviousAverageDeliveryDays = Math.Round(prevAvgDeliveryDays, 1),
            DeliveryTimeDistribution = deliveryTimeDistribution,
            DailyDeliveries = dailyDeliveries,
            DeliveryByState = deliveryByState
        };

        return Result<DeliveryPerformanceDto>.Success(result);
    }
}
