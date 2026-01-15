using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Query to get order statistics for dashboard.
/// </summary>
[RequirePermission("orders.view")]
[RequireFeature("order_management")]
public record GetOrderStatsQuery : IRequest<Result<OrderStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetOrderStatsQueryHandler : IRequestHandler<GetOrderStatsQuery, Result<OrderStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrderStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrderStatsDto>> Handle(
        GetOrderStatsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var query = _dbContext.Orders
            .Include(o => o.Channel)
            .AsNoTracking()
            .Where(o => o.DeletedAt == null &&
                        o.OrderDate >= fromDate &&
                        o.OrderDate <= toDate);

        var orders = await query.ToListAsync(cancellationToken);

        // Status counts
        var statusCounts = orders.GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Revenue calculations
        var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount.Amount);
        var avgOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalAmount.Amount) : 0;

        // COD stats
        var codOrders = orders.Where(o => o.PaymentMethod == PaymentMethod.COD).ToList();
        var codAmount = codOrders.Sum(o => o.TotalAmount.Amount);

        // By channel
        var ordersByChannel = orders
            .GroupBy(o => o.Channel?.Name ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var revenueByChannel = orders
            .Where(o => o.Status == OrderStatus.Delivered)
            .GroupBy(o => o.Channel?.Name ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount.Amount));

        // Daily stats
        var dailyStats = orders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyOrderStat
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount.Amount)
            })
            .ToList();

        var stats = new OrderStatsDto
        {
            TotalOrders = orders.Count,
            PendingOrders = statusCounts.GetValueOrDefault(OrderStatus.Pending),
            ConfirmedOrders = statusCounts.GetValueOrDefault(OrderStatus.Confirmed),
            ProcessingOrders = statusCounts.GetValueOrDefault(OrderStatus.Processing),
            ShippedOrders = statusCounts.GetValueOrDefault(OrderStatus.Shipped),
            DeliveredOrders = statusCounts.GetValueOrDefault(OrderStatus.Delivered),
            CancelledOrders = statusCounts.GetValueOrDefault(OrderStatus.Cancelled),
            RTOOrders = statusCounts.GetValueOrDefault(OrderStatus.RTO),
            ReturnedOrders = statusCounts.GetValueOrDefault(OrderStatus.Returned),
            TotalRevenue = totalRevenue,
            AverageOrderValue = avgOrderValue,
            TotalCODOrders = codOrders.Count,
            CODAmount = codAmount,
            OrdersByChannel = ordersByChannel,
            RevenueByChannel = revenueByChannel,
            DailyStats = dailyStats
        };

        return Result<OrderStatsDto>.Success(stats);
    }
}
