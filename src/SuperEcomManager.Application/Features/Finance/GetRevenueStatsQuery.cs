using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Query to get revenue statistics for a date range.
/// </summary>
[RequirePermission("finance.view")]
[RequireFeature("finance_management")]
public record GetRevenueStatsQuery : IRequest<Result<RevenueStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool IncludeDailyTrend { get; init; } = true;
    public int DailyTrendDays { get; init; } = 30;
}

public class GetRevenueStatsQueryHandler : IRequestHandler<GetRevenueStatsQuery, Result<RevenueStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetRevenueStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<RevenueStatsDto>> Handle(
        GetRevenueStatsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        // Get orders in date range
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Channel)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync(cancellationToken);

        var totalRevenue = orders.Sum(o => o.TotalAmount.Amount);
        var totalOrderCount = orders.Count;
        var averageOrderValue = totalOrderCount > 0 ? totalRevenue / totalOrderCount : 0;

        // By status
        var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var pendingOrders = orders.Where(o => o.Status == OrderStatus.Pending ||
                                              o.Status == OrderStatus.Confirmed ||
                                              o.Status == OrderStatus.Processing ||
                                              o.Status == OrderStatus.Shipped).ToList();
        var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Cancelled).ToList();
        var rtoOrders = orders.Where(o => o.Status == OrderStatus.RTO).ToList();

        var deliveredRevenue = deliveredOrders.Sum(o => o.TotalAmount.Amount);
        var pendingRevenue = pendingOrders.Sum(o => o.TotalAmount.Amount);
        var cancelledRevenue = cancelledOrders.Sum(o => o.TotalAmount.Amount);
        var rtoRevenue = rtoOrders.Sum(o => o.TotalAmount.Amount);

        // By payment method
        var prepaidOrders = orders.Where(o => !o.IsCOD).ToList();
        var codOrders = orders.Where(o => o.IsCOD).ToList();

        var prepaidRevenue = prepaidOrders.Sum(o => o.TotalAmount.Amount);
        var codRevenue = codOrders.Sum(o => o.TotalAmount.Amount);

        // By channel
        var revenueByChannel = orders
            .Where(o => o.Channel != null)
            .GroupBy(o => o.Channel!.Name)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount.Amount));

        // Daily trend
        var dailyRevenue = new List<DailyRevenueDto>();
        if (request.IncludeDailyTrend)
        {
            var trendStartDate = DateTime.UtcNow.AddDays(-request.DailyTrendDays).Date;
            var trendEndDate = DateTime.UtcNow.Date;

            var dailyOrders = orders
                .Where(o => o.OrderDate.Date >= trendStartDate)
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(o => o.TotalAmount.Amount), Count = g.Count() });

            // Fill in missing days with zero
            for (var date = trendStartDate; date <= trendEndDate; date = date.AddDays(1))
            {
                var dayData = dailyOrders.GetValueOrDefault(date);
                dailyRevenue.Add(new DailyRevenueDto
                {
                    Date = date,
                    Revenue = dayData?.Revenue ?? 0,
                    OrderCount = dayData?.Count ?? 0
                });
            }
        }

        var currency = orders.FirstOrDefault()?.TotalAmount.Currency ?? "INR";

        var stats = new RevenueStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalRevenue,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            Currency = currency,
            DeliveredRevenue = deliveredRevenue,
            PendingRevenue = pendingRevenue,
            CancelledRevenue = cancelledRevenue,
            RtoRevenue = rtoRevenue,
            PrepaidRevenue = prepaidRevenue,
            CodRevenue = codRevenue,
            TotalOrderCount = totalOrderCount,
            DeliveredOrderCount = deliveredOrders.Count,
            PendingOrderCount = pendingOrders.Count,
            CancelledOrderCount = cancelledOrders.Count,
            RtoOrderCount = rtoOrders.Count,
            RevenueByChannel = revenueByChannel,
            DailyRevenue = dailyRevenue
        };

        return Result<RevenueStatsDto>.Success(stats);
    }
}
