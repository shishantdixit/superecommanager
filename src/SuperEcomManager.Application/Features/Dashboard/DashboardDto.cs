namespace SuperEcomManager.Application.Features.Dashboard;

#region Overview DTOs

/// <summary>
/// Main dashboard overview with aggregated metrics.
/// </summary>
public record DashboardOverviewDto
{
    public DateTime AsOf { get; init; }
    public string Currency { get; init; } = "INR";

    // Today's summary
    public TodaySummaryDto Today { get; init; } = new();

    // Period summary (default: last 30 days)
    public PeriodSummaryDto Period { get; init; } = new();

    // Quick stats
    public QuickStatsDto QuickStats { get; init; } = new();

    // Trends
    public List<DailyTrendDto> DailyTrends { get; init; } = new();
}

/// <summary>
/// Today's activity summary.
/// </summary>
public record TodaySummaryDto
{
    public int NewOrders { get; init; }
    public decimal NewOrdersValue { get; init; }
    public int OrdersShipped { get; init; }
    public int OrdersDelivered { get; init; }
    public int NewNdrCases { get; init; }
    public int NdrResolved { get; init; }
    public decimal Revenue { get; init; }
    public decimal Expenses { get; init; }
}

/// <summary>
/// Period summary (configurable date range).
/// </summary>
public record PeriodSummaryDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    // Orders
    public int TotalOrders { get; init; }
    public decimal TotalOrderValue { get; init; }
    public decimal AverageOrderValue { get; init; }

    // Fulfillment
    public int OrdersFulfilled { get; init; }
    public int OrdersCancelled { get; init; }
    public int OrdersReturned { get; init; }
    public decimal FulfillmentRate { get; init; }

    // Delivery
    public int ShipmentsCreated { get; init; }
    public int ShipmentsDelivered { get; init; }
    public int ShipmentsRto { get; init; }
    public decimal DeliverySuccessRate { get; init; }

    // NDR
    public int NdrCasesCreated { get; init; }
    public int NdrCasesResolved { get; init; }
    public decimal NdrResolutionRate { get; init; }

    // Finance
    public decimal GrossRevenue { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ProfitMargin { get; init; }
}

/// <summary>
/// Quick stats for dashboard widgets.
/// </summary>
public record QuickStatsDto
{
    // Inventory
    public int TotalProducts { get; init; }
    public int LowStockProducts { get; init; }
    public int OutOfStockProducts { get; init; }

    // Pending items
    public int PendingOrders { get; init; }
    public int PendingShipments { get; init; }
    public int ActiveNdrCases { get; init; }

    // Channels
    public int ActiveChannels { get; init; }

    // Notifications
    public int NotificationsSentToday { get; init; }
    public int NotificationsFailed { get; init; }
}

/// <summary>
/// Daily trend data point.
/// </summary>
public record DailyTrendDto
{
    public DateTime Date { get; init; }
    public int Orders { get; init; }
    public decimal Revenue { get; init; }
    public int Shipments { get; init; }
    public int Deliveries { get; init; }
    public int NdrCases { get; init; }
}

#endregion

#region Orders Dashboard DTOs

/// <summary>
/// Orders-focused dashboard metrics.
/// </summary>
public record OrdersDashboardDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string Currency { get; init; } = "INR";

    // Summary
    public int TotalOrders { get; init; }
    public decimal TotalValue { get; init; }
    public decimal AverageOrderValue { get; init; }

    // By status
    public Dictionary<string, int> OrdersByStatus { get; init; } = new();
    public Dictionary<string, decimal> ValueByStatus { get; init; } = new();

    // By channel
    public List<ChannelOrdersDto> OrdersByChannel { get; init; } = new();

    // By payment method
    public int PrepaidOrders { get; init; }
    public decimal PrepaidValue { get; init; }
    public int CodOrders { get; init; }
    public decimal CodValue { get; init; }

    // Trends
    public List<DailyOrdersDto> DailyOrders { get; init; } = new();

    // Top products
    public List<TopProductDto> TopSellingProducts { get; init; } = new();
}

/// <summary>
/// Orders by channel breakdown.
/// </summary>
public record ChannelOrdersDto
{
    public Guid ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal OrderValue { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Daily orders data.
/// </summary>
public record DailyOrdersDto
{
    public DateTime Date { get; init; }
    public int Count { get; init; }
    public decimal Value { get; init; }
}

/// <summary>
/// Top selling product.
/// </summary>
public record TopProductDto
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}

#endregion

#region Shipments Dashboard DTOs

/// <summary>
/// Shipments-focused dashboard metrics.
/// </summary>
public record ShipmentsDashboardDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    // Summary
    public int TotalShipments { get; init; }
    public int Delivered { get; init; }
    public int InTransit { get; init; }
    public int OutForDelivery { get; init; }
    public int Rto { get; init; }
    public int Returned { get; init; }

    // Rates
    public decimal DeliveryRate { get; init; }
    public decimal RtoRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }

    // By status
    public Dictionary<string, int> ShipmentsByStatus { get; init; } = new();

    // By courier
    public List<CourierPerformanceDto> CourierPerformance { get; init; } = new();

    // NDR summary
    public int TotalNdrCases { get; init; }
    public int NdrPending { get; init; }
    public int NdrResolved { get; init; }
    public decimal NdrResolutionRate { get; init; }

    // Trends
    public List<DailyShipmentsDto> DailyShipments { get; init; } = new();
}

/// <summary>
/// Courier performance metrics.
/// </summary>
public record CourierPerformanceDto
{
    public string CourierName { get; init; } = string.Empty;
    public int TotalShipments { get; init; }
    public int Delivered { get; init; }
    public int Rto { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }
}

/// <summary>
/// Daily shipments data.
/// </summary>
public record DailyShipmentsDto
{
    public DateTime Date { get; init; }
    public int Created { get; init; }
    public int Delivered { get; init; }
    public int Rto { get; init; }
}

#endregion

#region Alerts DTOs

/// <summary>
/// Dashboard alerts requiring attention.
/// </summary>
public record DashboardAlertsDto
{
    public int TotalAlerts { get; init; }
    public List<AlertItemDto> CriticalAlerts { get; init; } = new();
    public List<AlertItemDto> WarningAlerts { get; init; } = new();
    public List<AlertItemDto> InfoAlerts { get; init; } = new();
}

/// <summary>
/// Individual alert item.
/// </summary>
public record AlertItemDto
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int Count { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
