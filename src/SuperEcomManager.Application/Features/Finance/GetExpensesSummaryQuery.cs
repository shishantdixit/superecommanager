using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Query to get expenses summary for a date range.
/// </summary>
[RequirePermission("finance.view")]
[RequireFeature("finance_management")]
public record GetExpensesSummaryQuery : IRequest<Result<ExpensesSummaryDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public ExpenseCategory? Category { get; init; }
    public bool IncludeDailyTrend { get; init; } = true;
    public int DailyTrendDays { get; init; } = 30;
    public int TopExpensesCount { get; init; } = 10;
}

public class GetExpensesSummaryQueryHandler : IRequestHandler<GetExpensesSummaryQuery, Result<ExpensesSummaryDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetExpensesSummaryQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ExpensesSummaryDto>> Handle(
        GetExpensesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        var query = _dbContext.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate);

        if (request.Category.HasValue)
        {
            query = query.Where(e => e.Category == request.Category.Value);
        }

        var expenses = await query.ToListAsync(cancellationToken);

        var totalExpenses = expenses.Sum(e => e.Amount.Amount);
        var totalExpenseCount = expenses.Count;

        // By category
        var expensesByCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key.ToString(), g => g.Sum(e => e.Amount.Amount));

        var countByCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        // Top expenses
        var topExpenses = expenses
            .OrderByDescending(e => e.Amount.Amount)
            .Take(request.TopExpensesCount)
            .Select(e => new ExpenseListDto
            {
                Id = e.Id,
                Category = e.Category,
                Amount = e.Amount.Amount,
                Currency = e.Amount.Currency,
                Description = e.Description,
                ExpenseDate = e.ExpenseDate,
                Vendor = e.Vendor,
                InvoiceNumber = e.InvoiceNumber,
                IsRecurring = e.IsRecurring,
                CreatedAt = e.CreatedAt
            })
            .ToList();

        // Daily trend
        var dailyExpenses = new List<DailyExpenseDto>();
        if (request.IncludeDailyTrend)
        {
            var trendStartDate = DateTime.UtcNow.AddDays(-request.DailyTrendDays).Date;
            var trendEndDate = DateTime.UtcNow.Date;

            var dailyData = expenses
                .Where(e => e.ExpenseDate.Date >= trendStartDate)
                .GroupBy(e => e.ExpenseDate.Date)
                .ToDictionary(g => g.Key, g => new { Amount = g.Sum(e => e.Amount.Amount), Count = g.Count() });

            // Fill in missing days with zero
            for (var date = trendStartDate; date <= trendEndDate; date = date.AddDays(1))
            {
                var dayData = dailyData.GetValueOrDefault(date);
                dailyExpenses.Add(new DailyExpenseDto
                {
                    Date = date,
                    Amount = dayData?.Amount ?? 0,
                    Count = dayData?.Count ?? 0
                });
            }
        }

        var currency = expenses.FirstOrDefault()?.Amount.Currency ?? "INR";

        var summary = new ExpensesSummaryDto
        {
            TotalExpenses = totalExpenses,
            Currency = currency,
            TotalExpenseCount = totalExpenseCount,
            ExpensesByCategory = expensesByCategory,
            CountByCategory = countByCategory,
            TopExpenses = topExpenses,
            DailyExpenses = dailyExpenses
        };

        return Result<ExpensesSummaryDto>.Success(summary);
    }
}
