using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Query to get profit and loss report for a date range.
/// </summary>
[RequirePermission("finance.view")]
[RequireFeature("finance_management")]
public record GetProfitLossReportQuery : IRequest<Result<ProfitLossReportDto>>, ITenantRequest
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool IncludeMonthlyTrend { get; init; } = true;
}

public class GetProfitLossReportQueryHandler : IRequestHandler<GetProfitLossReportQuery, Result<ProfitLossReportDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetProfitLossReportQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ProfitLossReportDto>> Handle(
        GetProfitLossReportQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date.AddDays(1).AddTicks(-1);

        // Get orders in date range
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync(cancellationToken);

        // Get all products for cost lookup
        var productIds = orders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        // Get expenses in date range
        var expenses = await _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate)
            .ToListAsync(cancellationToken);

        // Calculate revenue
        var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Cancelled).ToList();
        var returnedOrders = orders.Where(o => o.Status == OrderStatus.Returned).ToList();
        var rtoOrders = orders.Where(o => o.Status == OrderStatus.RTO).ToList();

        var grossRevenue = orders.Sum(o => o.TotalAmount.Amount);
        var discounts = orders.Sum(o => o.DiscountAmount.Amount);
        var returns = returnedOrders.Sum(o => o.TotalAmount.Amount);
        var netRevenue = deliveredOrders.Sum(o => o.TotalAmount.Amount);

        // Calculate cost of goods sold
        decimal costOfGoodsSold = 0;
        foreach (var order in deliveredOrders)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductId.HasValue && products.TryGetValue(item.ProductId.Value, out var product))
                {
                    costOfGoodsSold += product.CostPrice.Amount * item.Quantity;
                }
            }
        }

        var grossProfit = netRevenue - costOfGoodsSold;
        var grossProfitMargin = netRevenue > 0 ? (grossProfit / netRevenue) * 100 : 0;

        // Group expenses by category
        var expensesByCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount.Amount));

        var shippingExpenses = expensesByCategory.GetValueOrDefault(ExpenseCategory.Shipping, 0);
        var platformFees = expensesByCategory.GetValueOrDefault(ExpenseCategory.PlatformFees, 0);
        var paymentProcessingFees = expensesByCategory.GetValueOrDefault(ExpenseCategory.PaymentProcessing, 0);
        var packagingExpenses = expensesByCategory.GetValueOrDefault(ExpenseCategory.Packaging, 0);
        var returnExpenses = expensesByCategory.GetValueOrDefault(ExpenseCategory.Returns, 0);
        var rtoExpenses = expensesByCategory.GetValueOrDefault(ExpenseCategory.RTO, 0);
        var marketingExpenses = expensesByCategory.GetValueOrDefault(ExpenseCategory.Marketing, 0);
        var otherExpenses = expenses
            .Where(e => !new[]
            {
                ExpenseCategory.Shipping, ExpenseCategory.PlatformFees,
                ExpenseCategory.PaymentProcessing, ExpenseCategory.Packaging,
                ExpenseCategory.Returns, ExpenseCategory.RTO, ExpenseCategory.Marketing
            }.Contains(e.Category))
            .Sum(e => e.Amount.Amount);

        var totalOperatingExpenses = expenses.Sum(e => e.Amount.Amount);
        var operatingProfit = grossProfit - totalOperatingExpenses;
        var operatingProfitMargin = netRevenue > 0 ? (operatingProfit / netRevenue) * 100 : 0;

        // Fulfillment rate
        var fulfillmentRate = orders.Count > 0
            ? (decimal)deliveredOrders.Count / orders.Count * 100
            : 0;

        // Build expense breakdown
        var expenseBreakdown = expensesByCategory.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value);

        // Monthly trend
        var monthlyTrend = new List<MonthlyProfitLossDto>();
        if (request.IncludeMonthlyTrend)
        {
            var monthlyOrders = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            var monthlyExpenses = expenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount.Amount));

            foreach (var monthGroup in monthlyOrders)
            {
                var monthDelivered = monthGroup.Where(o => o.Status == OrderStatus.Delivered).ToList();
                var monthRevenue = monthDelivered.Sum(o => o.TotalAmount.Amount);
                var monthExpenseKey = new { monthGroup.Key.Year, monthGroup.Key.Month };
                var monthExpenseAmount = monthlyExpenses.GetValueOrDefault(monthExpenseKey, 0);
                var monthProfit = monthRevenue - monthExpenseAmount;
                var monthMargin = monthRevenue > 0 ? (monthProfit / monthRevenue) * 100 : 0;

                monthlyTrend.Add(new MonthlyProfitLossDto
                {
                    Year = monthGroup.Key.Year,
                    Month = monthGroup.Key.Month,
                    MonthName = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1)
                        .ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    Revenue = monthRevenue,
                    Expenses = monthExpenseAmount,
                    Profit = monthProfit,
                    ProfitMargin = Math.Round(monthMargin, 2),
                    OrderCount = monthGroup.Count()
                });
            }
        }

        var currency = orders.FirstOrDefault()?.TotalAmount.Currency ?? "INR";

        var report = new ProfitLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Currency = currency,
            GrossRevenue = grossRevenue,
            Discounts = discounts,
            Returns = returns,
            NetRevenue = netRevenue,
            CostOfGoodsSold = costOfGoodsSold,
            GrossProfit = grossProfit,
            GrossProfitMargin = Math.Round(grossProfitMargin, 2),
            ShippingExpenses = shippingExpenses,
            PlatformFees = platformFees,
            PaymentProcessingFees = paymentProcessingFees,
            PackagingExpenses = packagingExpenses,
            ReturnExpenses = returnExpenses,
            RtoExpenses = rtoExpenses,
            MarketingExpenses = marketingExpenses,
            OtherExpenses = otherExpenses,
            TotalOperatingExpenses = totalOperatingExpenses,
            OperatingProfit = operatingProfit,
            OperatingProfitMargin = Math.Round(operatingProfitMargin, 2),
            TotalOrders = orders.Count,
            DeliveredOrders = deliveredOrders.Count,
            CancelledOrders = cancelledOrders.Count,
            ReturnedOrders = returnedOrders.Count,
            RtoOrders = rtoOrders.Count,
            FulfillmentRate = Math.Round(fulfillmentRate, 2),
            ExpenseBreakdown = expenseBreakdown,
            MonthlyTrend = monthlyTrend
        };

        return Result<ProfitLossReportDto>.Success(report);
    }
}
