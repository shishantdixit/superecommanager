using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Dashboard;

/// <summary>
/// Query to get orders-focused dashboard metrics.
/// </summary>
[RequirePermission("dashboard.view")]
[RequireFeature("dashboard")]
public record GetOrdersDashboardQuery : IRequest<Result<OrdersDashboardDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int TopProductsCount { get; init; } = 10;
}

public class GetOrdersDashboardQueryHandler : IRequestHandler<GetOrdersDashboardQuery, Result<OrdersDashboardDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrdersDashboardQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrdersDashboardDto>> Handle(
        GetOrdersDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        // Get orders with items and channel
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Channel)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalValue = orders.Sum(o => o.TotalAmount.Amount);
        var averageOrderValue = totalOrders > 0 ? totalValue / totalOrders : 0;

        // By status
        var ordersByStatus = orders
            .GroupBy(o => o.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var valueByStatus = orders
            .GroupBy(o => o.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount.Amount));

        // By channel
        var ordersByChannel = orders
            .Where(o => o.Channel != null)
            .GroupBy(o => new { o.ChannelId, o.Channel!.Name, o.Channel.Type })
            .Select(g => new ChannelOrdersDto
            {
                ChannelId = g.Key.ChannelId,
                ChannelName = g.Key.Name,
                Platform = g.Key.Type.ToString(),
                OrderCount = g.Count(),
                OrderValue = g.Sum(o => o.TotalAmount.Amount),
                Percentage = totalOrders > 0 ? Math.Round((decimal)g.Count() / totalOrders * 100, 2) : 0
            })
            .OrderByDescending(c => c.OrderCount)
            .ToList();

        // By payment method
        var prepaidOrders = orders.Where(o => !o.IsCOD).ToList();
        var codOrders = orders.Where(o => o.IsCOD).ToList();

        // Daily orders
        var dailyOrders = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailyOrdersDto
            {
                Date = g.Key,
                Count = g.Count(),
                Value = g.Sum(o => o.TotalAmount.Amount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing days
        var filledDailyOrders = new List<DailyOrdersDto>();
        var dailyOrdersDict = dailyOrders.ToDictionary(d => d.Date);
        for (var date = fromDate; date <= toDate.Date; date = date.AddDays(1))
        {
            filledDailyOrders.Add(dailyOrdersDict.GetValueOrDefault(date) ?? new DailyOrdersDto
            {
                Date = date,
                Count = 0,
                Value = 0
            });
        }

        // Top selling products
        var productSales = orders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => new { i.ProductId, i.Sku, i.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId!.Value,
                Sku = g.Key.Sku,
                Name = g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalAmount.Amount)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(request.TopProductsCount)
            .ToList();

        var currency = orders.FirstOrDefault()?.TotalAmount.Currency ?? "INR";

        var dashboard = new OrdersDashboardDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Currency = currency,
            TotalOrders = totalOrders,
            TotalValue = totalValue,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            OrdersByStatus = ordersByStatus,
            ValueByStatus = valueByStatus,
            OrdersByChannel = ordersByChannel,
            PrepaidOrders = prepaidOrders.Count,
            PrepaidValue = prepaidOrders.Sum(o => o.TotalAmount.Amount),
            CodOrders = codOrders.Count,
            CodValue = codOrders.Sum(o => o.TotalAmount.Amount),
            DailyOrders = filledDailyOrders,
            TopSellingProducts = productSales
        };

        return Result<OrdersDashboardDto>.Success(dashboard);
    }
}
