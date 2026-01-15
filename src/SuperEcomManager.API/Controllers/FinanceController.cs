using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Finance;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Finance and P&L management endpoints.
/// </summary>
[Authorize]
public class FinanceController : ApiControllerBase
{
    #region Profit & Loss

    /// <summary>
    /// Get profit and loss report for a date range.
    /// </summary>
    [HttpGet("profit-loss")]
    [ProducesResponseType(typeof(ApiResponse<ProfitLossReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ProfitLossReportDto>>> GetProfitLossReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] bool includeMonthlyTrend = true)
    {
        var result = await Mediator.Send(new GetProfitLossReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            IncludeMonthlyTrend = includeMonthlyTrend
        });

        if (result.IsFailure)
            return BadRequestResponse<ProfitLossReportDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    #endregion

    #region Revenue Stats

    /// <summary>
    /// Get revenue statistics.
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevenueStatsDto>>> GetRevenueStats(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool includeDailyTrend = true,
        [FromQuery] int dailyTrendDays = 30)
    {
        var result = await Mediator.Send(new GetRevenueStatsQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            IncludeDailyTrend = includeDailyTrend,
            DailyTrendDays = dailyTrendDays
        });

        if (result.IsFailure)
            return BadRequestResponse<RevenueStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    #endregion

    #region Expenses

    /// <summary>
    /// Get paginated list of expenses.
    /// </summary>
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ExpenseListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ExpenseListDto>>>> GetExpenses(
        [FromQuery] ExpenseCategory? category,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? vendor,
        [FromQuery] bool? isRecurring,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool sortDescending = true)
    {
        var result = await Mediator.Send(new GetExpensesQuery
        {
            Filter = new ExpenseFilterDto
            {
                Category = category,
                FromDate = fromDate,
                ToDate = toDate,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                Vendor = vendor,
                IsRecurring = isRecurring,
                SearchTerm = searchTerm
            },
            Page = page,
            PageSize = pageSize,
            SortDescending = sortDescending
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<ExpenseListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get expenses summary with category breakdown.
    /// </summary>
    [HttpGet("expenses/summary")]
    [ProducesResponseType(typeof(ApiResponse<ExpensesSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ExpensesSummaryDto>>> GetExpensesSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] ExpenseCategory? category,
        [FromQuery] bool includeDailyTrend = true,
        [FromQuery] int dailyTrendDays = 30,
        [FromQuery] int topExpensesCount = 10)
    {
        var result = await Mediator.Send(new GetExpensesSummaryQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            Category = category,
            IncludeDailyTrend = includeDailyTrend,
            DailyTrendDays = dailyTrendDays,
            TopExpensesCount = topExpensesCount
        });

        if (result.IsFailure)
            return BadRequestResponse<ExpensesSummaryDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Record a new expense.
    /// </summary>
    [HttpPost("expenses")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ExpenseDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ExpenseDetailDto>>> RecordExpense(
        [FromBody] CreateExpenseRequest request)
    {
        var result = await Mediator.Send(new RecordExpenseCommand
        {
            Category = request.Category,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            Vendor = request.Vendor,
            InvoiceNumber = request.InvoiceNumber,
            Notes = request.Notes,
            IsRecurring = request.IsRecurring
        });

        if (result.IsFailure)
            return BadRequestResponse<ExpenseDetailDto>(string.Join(", ", result.Errors));

        return CreatedResponse($"/api/finance/expenses/{result.Value!.Id}", result.Value!);
    }

    #endregion

    #region Order Financials

    /// <summary>
    /// Get financial details for a specific order.
    /// </summary>
    [HttpGet("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderFinancialsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderFinancialsDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderFinancialsDto>>> GetOrderFinancials(Guid orderId)
    {
        var result = await Mediator.Send(new GetOrderFinancialsQuery { OrderId = orderId });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<OrderFinancialsDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<OrderFinancialsDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Request to create an expense.
/// </summary>
public record CreateExpenseRequest
{
    public ExpenseCategory Category { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Description { get; init; } = string.Empty;
    public DateTime ExpenseDate { get; init; }
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? Vendor { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
}

#endregion
