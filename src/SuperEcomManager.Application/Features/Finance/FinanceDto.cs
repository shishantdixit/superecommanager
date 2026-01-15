using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Finance;

#region Expense DTOs

/// <summary>
/// DTO for expense list items.
/// </summary>
public record ExpenseListDto
{
    public Guid Id { get; init; }
    public ExpenseCategory Category { get; init; }
    public string CategoryName => Category.ToString();
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Description { get; init; } = string.Empty;
    public DateTime ExpenseDate { get; init; }
    public string? Vendor { get; init; }
    public string? InvoiceNumber { get; init; }
    public bool IsRecurring { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for expense details.
/// </summary>
public record ExpenseDetailDto
{
    public Guid Id { get; init; }
    public ExpenseCategory Category { get; init; }
    public string CategoryName => Category.ToString();
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
    public Guid? RecordedByUserId { get; init; }
    public string? RecordedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating an expense.
/// </summary>
public record CreateExpenseDto
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

/// <summary>
/// DTO for updating an expense.
/// </summary>
public record UpdateExpenseDto
{
    public ExpenseCategory Category { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Description { get; init; } = string.Empty;
    public DateTime ExpenseDate { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Filter for expense queries.
/// </summary>
public record ExpenseFilterDto
{
    public ExpenseCategory? Category { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? Vendor { get; init; }
    public bool? IsRecurring { get; init; }
    public string? SearchTerm { get; init; }
}

#endregion

#region Revenue & P&L DTOs

/// <summary>
/// Summary of revenue statistics.
/// </summary>
public record RevenueStatsDto
{
    public decimal TotalRevenue { get; init; }
    public decimal TotalOrders { get; init; }
    public decimal AverageOrderValue { get; init; }
    public string Currency { get; init; } = "INR";

    // By status
    public decimal DeliveredRevenue { get; init; }
    public decimal PendingRevenue { get; init; }
    public decimal CancelledRevenue { get; init; }
    public decimal RtoRevenue { get; init; }

    // By payment method
    public decimal PrepaidRevenue { get; init; }
    public decimal CodRevenue { get; init; }

    // Order counts
    public int TotalOrderCount { get; init; }
    public int DeliveredOrderCount { get; init; }
    public int PendingOrderCount { get; init; }
    public int CancelledOrderCount { get; init; }
    public int RtoOrderCount { get; init; }

    // By channel
    public Dictionary<string, decimal> RevenueByChannel { get; init; } = new();

    // Daily trend (last 30 days)
    public List<DailyRevenueDto> DailyRevenue { get; init; } = new();
}

/// <summary>
/// Daily revenue data point.
/// </summary>
public record DailyRevenueDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}

/// <summary>
/// Summary of expenses by category.
/// </summary>
public record ExpensesSummaryDto
{
    public decimal TotalExpenses { get; init; }
    public string Currency { get; init; } = "INR";
    public int TotalExpenseCount { get; init; }
    public Dictionary<string, decimal> ExpensesByCategory { get; init; } = new();
    public Dictionary<string, int> CountByCategory { get; init; } = new();
    public List<ExpenseListDto> TopExpenses { get; init; } = new();
    public List<DailyExpenseDto> DailyExpenses { get; init; } = new();
}

/// <summary>
/// Daily expense data point.
/// </summary>
public record DailyExpenseDto
{
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// Profit and Loss report.
/// </summary>
public record ProfitLossReportDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string Currency { get; init; } = "INR";

    // Revenue section
    public decimal GrossRevenue { get; init; }
    public decimal Discounts { get; init; }
    public decimal Returns { get; init; }
    public decimal NetRevenue { get; init; }

    // Cost of goods section
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossProfitMargin { get; init; }

    // Operating expenses
    public decimal ShippingExpenses { get; init; }
    public decimal PlatformFees { get; init; }
    public decimal PaymentProcessingFees { get; init; }
    public decimal PackagingExpenses { get; init; }
    public decimal ReturnExpenses { get; init; }
    public decimal RtoExpenses { get; init; }
    public decimal MarketingExpenses { get; init; }
    public decimal OtherExpenses { get; init; }
    public decimal TotalOperatingExpenses { get; init; }

    // Bottom line
    public decimal OperatingProfit { get; init; }
    public decimal OperatingProfitMargin { get; init; }

    // Order metrics
    public int TotalOrders { get; init; }
    public int DeliveredOrders { get; init; }
    public int CancelledOrders { get; init; }
    public int ReturnedOrders { get; init; }
    public int RtoOrders { get; init; }
    public decimal FulfillmentRate { get; init; }

    // Breakdown by category
    public Dictionary<string, decimal> ExpenseBreakdown { get; init; } = new();

    // Monthly trend
    public List<MonthlyProfitLossDto> MonthlyTrend { get; init; } = new();
}

/// <summary>
/// Monthly P&L summary.
/// </summary>
public record MonthlyProfitLossDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public decimal Expenses { get; init; }
    public decimal Profit { get; init; }
    public decimal ProfitMargin { get; init; }
    public int OrderCount { get; init; }
}

#endregion

#region Order Financials DTOs

/// <summary>
/// Financial details for an order.
/// </summary>
public record OrderFinancialsDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    // Revenue
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingCharged { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";

    // Costs
    public decimal CostOfGoods { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal PlatformFee { get; init; }
    public decimal PaymentProcessingFee { get; init; }
    public decimal PackagingCost { get; init; }
    public decimal TotalCosts { get; init; }

    // Profit
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ProfitMargin { get; init; }

    // Item breakdown
    public List<OrderItemFinancialsDto> Items { get; init; } = new();

    // Associated expenses
    public List<ExpenseListDto> AssociatedExpenses { get; init; } = new();
}

/// <summary>
/// Financial details for an order item.
/// </summary>
public record OrderItemFinancialsDto
{
    public Guid ItemId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal TotalCost { get; init; }
    public decimal ItemProfit { get; init; }
    public decimal ProfitMargin { get; init; }
}

/// <summary>
/// Filter for order financials query.
/// </summary>
public record OrderFinancialsFilterDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? ChannelId { get; init; }
    public string? Status { get; init; }
    public bool? IsProfitable { get; init; }
}

/// <summary>
/// Summary of order financials.
/// </summary>
public record OrderFinancialsSummaryDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string Currency { get; init; } = "INR";

    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalCosts { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal AverageProfit { get; init; }
    public decimal AverageProfitMargin { get; init; }

    public int ProfitableOrders { get; init; }
    public int UnprofitableOrders { get; init; }

    public List<OrderFinancialsDto> TopProfitableOrders { get; init; } = new();
    public List<OrderFinancialsDto> LeastProfitableOrders { get; init; } = new();
}

#endregion
