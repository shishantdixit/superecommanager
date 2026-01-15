using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Query to get order trends with status breakdown.
/// </summary>
[RequirePermission("analytics.view")]
[RequireFeature("analytics")]
public record GetOrderTrendsQuery : IRequest<Result<OrderTrendsDto>>, ITenantRequest
{
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Last30Days;
    public DateTime? CustomStartDate { get; init; }
    public DateTime? CustomEndDate { get; init; }
}

public class GetOrderTrendsQueryHandler : IRequestHandler<GetOrderTrendsQuery, Result<OrderTrendsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrderTrendsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrderTrendsDto>> Handle(
        GetOrderTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, prevStartDate, prevEndDate) = AnalyticsPeriodHelper.GetDateRange(
            request.Period,
            request.CustomStartDate,
            request.CustomEndDate);

        // Current period orders
        var currentOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                o.Status
            })
            .ToListAsync(cancellationToken);

        // Previous period count
        var previousOrderCount = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate)
            .CountAsync(cancellationToken);

        var totalOrders = currentOrders.Count;
        var percentageChange = previousOrderCount > 0
            ? ((decimal)(totalOrders - previousOrderCount) / previousOrderCount) * 100
            : (totalOrders > 0 ? 100 : 0);

        // Daily orders
        var dailyOrders = currentOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailyOrderCountDto
            {
                Date = g.Key,
                OrderCount = g.Count(),
                ConfirmedCount = g.Count(o => o.Status == OrderStatus.Confirmed ||
                                              o.Status == OrderStatus.Processing ||
                                              o.Status == OrderStatus.Shipped ||
                                              o.Status == OrderStatus.Delivered),
                CancelledCount = g.Count(o => o.Status == OrderStatus.Cancelled)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Orders by status
        var ordersByStatus = currentOrders
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCountDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalOrders > 0 ? ((decimal)g.Count() / totalOrders) * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // Orders by hour
        var ordersByHour = currentOrders
            .GroupBy(o => o.OrderDate.Hour)
            .Select(g => new HourlyOrderCountDto
            {
                Hour = g.Key,
                OrderCount = g.Count()
            })
            .OrderBy(h => h.Hour)
            .ToList();

        // Fill missing hours with 0
        var allHours = Enumerable.Range(0, 24)
            .Select(h => ordersByHour.FirstOrDefault(oh => oh.Hour == h) ?? new HourlyOrderCountDto { Hour = h, OrderCount = 0 })
            .ToList();

        // Calculate averages and peaks
        var daysInPeriod = (endDate - startDate).Days + 1;
        var avgOrdersPerDay = daysInPeriod > 0 ? (decimal)totalOrders / daysInPeriod : 0;

        var peakHour = allHours.OrderByDescending(h => h.OrderCount).FirstOrDefault()?.Hour ?? 0;

        var peakDay = dailyOrders.OrderByDescending(d => d.OrderCount).FirstOrDefault()?.Date.DayOfWeek.ToString() ?? "N/A";

        var result = new OrderTrendsDto
        {
            TotalOrders = totalOrders,
            PreviousPeriodOrders = previousOrderCount,
            PercentageChange = Math.Round(percentageChange, 2),
            DailyOrders = dailyOrders,
            OrdersByStatus = ordersByStatus,
            OrdersByHour = allHours,
            AverageOrdersPerDay = Math.Round(avgOrdersPerDay, 2),
            PeakHour = peakHour,
            PeakDay = peakDay
        };

        return Result<OrderTrendsDto>.Success(result);
    }
}
