using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Query to get revenue trends with period comparison.
/// </summary>
[RequirePermission("analytics.view")]
[RequireFeature("analytics")]
public record GetRevenueTrendsQuery : IRequest<Result<RevenueTrendsDto>>, ITenantRequest
{
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Last30Days;
    public DateTime? CustomStartDate { get; init; }
    public DateTime? CustomEndDate { get; init; }
}

public class GetRevenueTrendsQueryHandler : IRequestHandler<GetRevenueTrendsQuery, Result<RevenueTrendsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetRevenueTrendsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<RevenueTrendsDto>> Handle(
        GetRevenueTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, prevStartDate, prevEndDate) = AnalyticsPeriodHelper.GetDateRange(
            request.Period,
            request.CustomStartDate,
            request.CustomEndDate);

        // Current period orders (exclude cancelled)
        var currentOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                TotalAmount = o.TotalAmount.Amount,
                o.ChannelId,
                o.PaymentMethod
            })
            .ToListAsync(cancellationToken);

        // Previous period orders
        var previousOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                TotalAmount = o.TotalAmount.Amount
            })
            .ToListAsync(cancellationToken);

        // Calculate totals
        var totalRevenue = currentOrders.Sum(o => o.TotalAmount);
        var previousRevenue = previousOrders.Sum(o => o.TotalAmount);
        var totalOrders = currentOrders.Count;
        var previousOrderCount = previousOrders.Count;

        var percentageChange = previousRevenue > 0
            ? ((totalRevenue - previousRevenue) / previousRevenue) * 100
            : (totalRevenue > 0 ? 100 : 0);

        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        var prevAvgOrderValue = previousOrderCount > 0 ? previousRevenue / previousOrderCount : 0;

        // Daily revenue
        var dailyRevenue = currentOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                AverageOrderValue = g.Count() > 0 ? g.Sum(o => o.TotalAmount) / g.Count() : 0
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Get channel names
        var channelIds = currentOrders.Select(o => o.ChannelId).Distinct().ToList();
        var channels = await _dbContext.SalesChannels
            .AsNoTracking()
            .Where(c => channelIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Type })
            .ToListAsync(cancellationToken);

        // Revenue by channel
        var revenueByChannel = currentOrders
            .GroupBy(o => o.ChannelId)
            .Select(g =>
            {
                var channel = channels.FirstOrDefault(c => c.Id == g.Key);
                return new ChannelRevenueDto
                {
                    ChannelId = g.Key,
                    ChannelName = channel?.Name ?? "Unknown",
                    ChannelType = channel?.Type.ToString() ?? "Unknown",
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    Percentage = totalRevenue > 0 ? (g.Sum(o => o.TotalAmount) / totalRevenue) * 100 : 0
                };
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // Revenue by payment method
        var revenueByPaymentMethod = currentOrders
            .GroupBy(o => o.PaymentMethod ?? PaymentMethod.Other)
            .Select(g => new PaymentMethodRevenueDto
            {
                PaymentMethod = g.Key.ToString(),
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                Percentage = totalRevenue > 0 ? (g.Sum(o => o.TotalAmount) / totalRevenue) * 100 : 0
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        var result = new RevenueTrendsDto
        {
            TotalRevenue = totalRevenue,
            PreviousPeriodRevenue = previousRevenue,
            PercentageChange = Math.Round(percentageChange, 2),
            TotalOrders = totalOrders,
            PreviousPeriodOrders = previousOrderCount,
            AverageOrderValue = Math.Round(avgOrderValue, 2),
            PreviousAverageOrderValue = Math.Round(prevAvgOrderValue, 2),
            DailyRevenue = dailyRevenue,
            RevenueByChannel = revenueByChannel,
            RevenueByPaymentMethod = revenueByPaymentMethod
        };

        return Result<RevenueTrendsDto>.Success(result);
    }
}
