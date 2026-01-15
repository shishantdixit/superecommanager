using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Dashboard;

/// <summary>
/// Query to get shipments-focused dashboard metrics.
/// </summary>
[RequirePermission("dashboard.view")]
[RequireFeature("dashboard")]
public record GetShipmentsDashboardQuery : IRequest<Result<ShipmentsDashboardDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetShipmentsDashboardQueryHandler : IRequestHandler<GetShipmentsDashboardQuery, Result<ShipmentsDashboardDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetShipmentsDashboardQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ShipmentsDashboardDto>> Handle(
        GetShipmentsDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        // Get shipments
        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var totalShipments = shipments.Count;
        var delivered = shipments.Count(s => s.Status == ShipmentStatus.Delivered);
        var inTransit = shipments.Count(s => s.Status == ShipmentStatus.InTransit);
        var outForDelivery = shipments.Count(s => s.Status == ShipmentStatus.OutForDelivery);
        var rto = shipments.Count(s => s.Status == ShipmentStatus.RTOInitiated || s.Status == ShipmentStatus.RTOInTransit || s.Status == ShipmentStatus.RTODelivered);
        var returned = shipments.Count(s => s.Status == ShipmentStatus.RTODelivered);

        // Rates
        var deliveryRate = totalShipments > 0 ? (decimal)delivered / totalShipments * 100 : 0;
        var rtoRate = totalShipments > 0 ? (decimal)rto / totalShipments * 100 : 0;

        // Average delivery days
        var deliveredShipments = shipments.Where(s =>
            s.Status == ShipmentStatus.Delivered &&
            s.DeliveredAt.HasValue).ToList();

        var averageDeliveryDays = deliveredShipments.Count > 0
            ? deliveredShipments.Average(s => (s.DeliveredAt!.Value - s.CreatedAt).TotalDays)
            : 0;

        // By status
        var shipmentsByStatus = shipments
            .GroupBy(s => s.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // By courier
        var courierPerformance = shipments
            .GroupBy(s => s.CourierName)
            .Select(g =>
            {
                var courierShipments = g.ToList();
                var courierDelivered = courierShipments.Count(s => s.Status == ShipmentStatus.Delivered);
                var courierRto = courierShipments.Count(s => s.Status == ShipmentStatus.RTOInitiated || s.Status == ShipmentStatus.RTOInTransit || s.Status == ShipmentStatus.RTODelivered);
                var courierTotal = courierShipments.Count;

                var courierDeliveredShipments = courierShipments.Where(s =>
                    s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue).ToList();
                var courierAvgDays = courierDeliveredShipments.Count > 0
                    ? courierDeliveredShipments.Average(s => (s.DeliveredAt!.Value - s.CreatedAt).TotalDays)
                    : 0;

                return new CourierPerformanceDto
                {
                    CourierName = g.Key,
                    TotalShipments = courierTotal,
                    Delivered = courierDelivered,
                    Rto = courierRto,
                    DeliveryRate = courierTotal > 0 ? Math.Round((decimal)courierDelivered / courierTotal * 100, 2) : 0,
                    AverageDeliveryDays = Math.Round((decimal)courierAvgDays, 1)
                };
            })
            .OrderByDescending(c => c.TotalShipments)
            .ToList();

        // NDR summary
        var ndrCases = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.CreatedAt >= fromDate && n.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var totalNdrCases = ndrCases.Count;
        var ndrPending = ndrCases.Count(n =>
            n.Status == NdrStatus.Open ||
            n.Status == NdrStatus.Assigned ||
            n.Status == NdrStatus.CustomerContacted ||
            n.Status == NdrStatus.Escalated);
        var ndrResolved = ndrCases.Count(n =>
            n.Status == NdrStatus.ClosedDelivered ||
            n.Status == NdrStatus.Delivered ||
            n.Status == NdrStatus.ClosedRTO ||
            n.Status == NdrStatus.ClosedAddressUpdated);
        var ndrResolutionRate = totalNdrCases > 0 ? (decimal)ndrResolved / totalNdrCases * 100 : 0;

        // Daily shipments
        var dailyShipments = new List<DailyShipmentsDto>();
        var shipmentsByDate = shipments.GroupBy(s => s.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var deliveriesByDate = shipments
            .Where(s => s.DeliveredAt.HasValue)
            .GroupBy(s => s.DeliveredAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var rtoByDate = shipments
            .Where(s => s.Status == ShipmentStatus.RTOInitiated || s.Status == ShipmentStatus.RTOInTransit || s.Status == ShipmentStatus.RTODelivered)
            .GroupBy(s => s.UpdatedAt?.Date ?? s.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        for (var date = fromDate; date <= toDate.Date; date = date.AddDays(1))
        {
            dailyShipments.Add(new DailyShipmentsDto
            {
                Date = date,
                Created = shipmentsByDate.GetValueOrDefault(date, 0),
                Delivered = deliveriesByDate.GetValueOrDefault(date, 0),
                Rto = rtoByDate.GetValueOrDefault(date, 0)
            });
        }

        var dashboard = new ShipmentsDashboardDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalShipments = totalShipments,
            Delivered = delivered,
            InTransit = inTransit,
            OutForDelivery = outForDelivery,
            Rto = rto,
            Returned = returned,
            DeliveryRate = Math.Round(deliveryRate, 2),
            RtoRate = Math.Round(rtoRate, 2),
            AverageDeliveryDays = Math.Round((decimal)averageDeliveryDays, 1),
            ShipmentsByStatus = shipmentsByStatus,
            CourierPerformance = courierPerformance,
            TotalNdrCases = totalNdrCases,
            NdrPending = ndrPending,
            NdrResolved = ndrResolved,
            NdrResolutionRate = Math.Round(ndrResolutionRate, 2),
            DailyShipments = dailyShipments
        };

        return Result<ShipmentsDashboardDto>.Success(dashboard);
    }
}
