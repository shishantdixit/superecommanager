using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Dashboard;

/// <summary>
/// Query to get main dashboard overview.
/// </summary>
[RequirePermission("dashboard.view")]
[RequireFeature("dashboard")]
public record GetDashboardOverviewQuery : IRequest<Result<DashboardOverviewDto>>, ITenantRequest
{
    public int PeriodDays { get; init; } = 30;
    public int TrendDays { get; init; } = 14;
}

public class GetDashboardOverviewQueryHandler : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetDashboardOverviewQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DashboardOverviewDto>> Handle(
        GetDashboardOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);
        var periodStart = now.AddDays(-request.PeriodDays).Date;
        var trendStart = now.AddDays(-request.TrendDays).Date;

        // Get today's data
        var todaySummary = await GetTodaySummary(todayStart, todayEnd, cancellationToken);

        // Get period summary
        var periodSummary = await GetPeriodSummary(periodStart, now, cancellationToken);

        // Get quick stats
        var quickStats = await GetQuickStats(todayStart, cancellationToken);

        // Get daily trends
        var dailyTrends = await GetDailyTrends(trendStart, now, cancellationToken);

        var currency = "INR";

        var overview = new DashboardOverviewDto
        {
            AsOf = now,
            Currency = currency,
            Today = todaySummary,
            Period = periodSummary,
            QuickStats = quickStats,
            DailyTrends = dailyTrends
        };

        return Result<DashboardOverviewDto>.Success(overview);
    }

    private async Task<TodaySummaryDto> GetTodaySummary(
        DateTime todayStart,
        DateTime todayEnd,
        CancellationToken cancellationToken)
    {
        // Today's orders
        var todayOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt <= todayEnd)
            .ToListAsync(cancellationToken);

        var newOrders = todayOrders.Count;
        var newOrdersValue = todayOrders.Sum(o => o.TotalAmount.Amount);

        // Today's shipments
        var shippedToday = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o => o.ShippedAt >= todayStart && o.ShippedAt <= todayEnd, cancellationToken);

        var deliveredToday = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o => o.DeliveredAt >= todayStart && o.DeliveredAt <= todayEnd, cancellationToken);

        // Today's NDR
        var todayNdr = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.CreatedAt >= todayStart && n.CreatedAt <= todayEnd)
            .ToListAsync(cancellationToken);

        var newNdrCases = todayNdr.Count;
        var ndrResolved = todayNdr.Count(n =>
            n.Status == NdrStatus.ClosedDelivered ||
            n.Status == NdrStatus.Delivered ||
            n.Status == NdrStatus.ClosedRTO ||
            n.Status == NdrStatus.ClosedAddressUpdated);

        // Today's finance
        var todayDeliveredOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.DeliveredAt >= todayStart && o.DeliveredAt <= todayEnd)
            .SumAsync(o => o.TotalAmount.Amount, cancellationToken);

        var todayExpenses = await _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= todayStart && e.ExpenseDate <= todayEnd)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        return new TodaySummaryDto
        {
            NewOrders = newOrders,
            NewOrdersValue = newOrdersValue,
            OrdersShipped = shippedToday,
            OrdersDelivered = deliveredToday,
            NewNdrCases = newNdrCases,
            NdrResolved = ndrResolved,
            Revenue = todayDeliveredOrders,
            Expenses = todayExpenses
        };
    }

    private async Task<PeriodSummaryDto> GetPeriodSummary(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        // Orders
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalOrderValue = orders.Sum(o => o.TotalAmount.Amount);
        var averageOrderValue = totalOrders > 0 ? totalOrderValue / totalOrders : 0;

        var ordersFulfilled = orders.Count(o => o.Status == OrderStatus.Delivered);
        var ordersCancelled = orders.Count(o => o.Status == OrderStatus.Cancelled);
        var ordersReturned = orders.Count(o => o.Status == OrderStatus.Returned || o.Status == OrderStatus.RTO);
        var fulfillmentRate = totalOrders > 0 ? (decimal)ordersFulfilled / totalOrders * 100 : 0;

        // Shipments
        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var shipmentsCreated = shipments.Count;
        var shipmentsDelivered = shipments.Count(s => s.Status == ShipmentStatus.Delivered);
        var shipmentsRto = shipments.Count(s => s.Status == ShipmentStatus.RTOInitiated || s.Status == ShipmentStatus.RTOInTransit || s.Status == ShipmentStatus.RTODelivered);
        var deliverySuccessRate = shipmentsCreated > 0 ? (decimal)shipmentsDelivered / shipmentsCreated * 100 : 0;

        // NDR
        var ndrCases = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.CreatedAt >= fromDate && n.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var ndrCreated = ndrCases.Count;
        var ndrResolved = ndrCases.Count(n =>
            n.Status == NdrStatus.ClosedDelivered ||
            n.Status == NdrStatus.Delivered ||
            n.Status == NdrStatus.ClosedRTO ||
            n.Status == NdrStatus.ClosedAddressUpdated);
        var ndrResolutionRate = ndrCreated > 0 ? (decimal)ndrResolved / ndrCreated * 100 : 0;

        // Finance
        var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var grossRevenue = deliveredOrders.Sum(o => o.TotalAmount.Amount);

        var expenses = await _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        var netProfit = grossRevenue - expenses;
        var profitMargin = grossRevenue > 0 ? netProfit / grossRevenue * 100 : 0;

        return new PeriodSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalOrders = totalOrders,
            TotalOrderValue = totalOrderValue,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            OrdersFulfilled = ordersFulfilled,
            OrdersCancelled = ordersCancelled,
            OrdersReturned = ordersReturned,
            FulfillmentRate = Math.Round(fulfillmentRate, 2),
            ShipmentsCreated = shipmentsCreated,
            ShipmentsDelivered = shipmentsDelivered,
            ShipmentsRto = shipmentsRto,
            DeliverySuccessRate = Math.Round(deliverySuccessRate, 2),
            NdrCasesCreated = ndrCreated,
            NdrCasesResolved = ndrResolved,
            NdrResolutionRate = Math.Round(ndrResolutionRate, 2),
            GrossRevenue = grossRevenue,
            TotalExpenses = expenses,
            NetProfit = netProfit,
            ProfitMargin = Math.Round(profitMargin, 2)
        };
    }

    private async Task<QuickStatsDto> GetQuickStats(DateTime todayStart, CancellationToken cancellationToken)
    {
        // Inventory
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .CountAsync(cancellationToken);

        var inventoryItems = await _dbContext.Inventory
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var lowStockProducts = inventoryItems
            .Where(i => i.IsLowStock() && i.QuantityOnHand > 0)
            .Select(i => i.ProductId)
            .Distinct()
            .Count();

        var outOfStockProducts = inventoryItems
            .Where(i => i.QuantityOnHand == 0)
            .Select(i => i.ProductId)
            .Distinct()
            .Count();

        // Pending items
        var pendingOrders = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed, cancellationToken);

        var pendingShipments = await _dbContext.Shipments
            .AsNoTracking()
            .CountAsync(s => s.Status == ShipmentStatus.Created || s.Status == ShipmentStatus.Manifested, cancellationToken);

        var activeNdrCases = await _dbContext.NdrRecords
            .AsNoTracking()
            .CountAsync(n => n.Status == NdrStatus.Open || n.Status == NdrStatus.Assigned || n.Status == NdrStatus.CustomerContacted || n.Status == NdrStatus.Escalated, cancellationToken);

        // Channels
        var activeChannels = await _dbContext.SalesChannels
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        // Notifications
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);
        var notificationsSentToday = await _dbContext.NotificationLogs
            .AsNoTracking()
            .CountAsync(n => n.CreatedAt >= todayStart && n.CreatedAt <= todayEnd &&
                           (n.Status == "Sent" || n.Status == "Delivered"), cancellationToken);

        var notificationsFailed = await _dbContext.NotificationLogs
            .AsNoTracking()
            .CountAsync(n => n.CreatedAt >= todayStart && n.CreatedAt <= todayEnd &&
                           n.Status == "Failed", cancellationToken);

        return new QuickStatsDto
        {
            TotalProducts = products,
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            PendingOrders = pendingOrders,
            PendingShipments = pendingShipments,
            ActiveNdrCases = activeNdrCases,
            ActiveChannels = activeChannels,
            NotificationsSentToday = notificationsSentToday,
            NotificationsFailed = notificationsFailed
        };
    }

    private async Task<List<DailyTrendDto>> GetDailyTrends(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var trends = new List<DailyTrendDto>();

        // Get all relevant data
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync(cancellationToken);

        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var ndrCases = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.CreatedAt >= fromDate && n.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        // Group by date
        var ordersByDate = orders.GroupBy(o => o.OrderDate.Date)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Value = g.Sum(o => o.TotalAmount.Amount) });

        var shipmentsByDate = shipments.GroupBy(s => s.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var deliveriesByDate = orders.Where(o => o.DeliveredAt.HasValue)
            .GroupBy(o => o.DeliveredAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var ndrByDate = ndrCases.GroupBy(n => n.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // Build trend data
        for (var date = fromDate; date <= toDate.Date; date = date.AddDays(1))
        {
            var orderData = ordersByDate.GetValueOrDefault(date);
            trends.Add(new DailyTrendDto
            {
                Date = date,
                Orders = orderData?.Count ?? 0,
                Revenue = orderData?.Value ?? 0,
                Shipments = shipmentsByDate.GetValueOrDefault(date, 0),
                Deliveries = deliveriesByDate.GetValueOrDefault(date, 0),
                NdrCases = ndrByDate.GetValueOrDefault(date, 0)
            });
        }

        return trends;
    }
}
