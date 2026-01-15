namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Revenue trends DTO with period comparison.
/// </summary>
public record RevenueTrendsDto
{
    public decimal TotalRevenue { get; init; }
    public decimal PreviousPeriodRevenue { get; init; }
    public decimal PercentageChange { get; init; }
    public int TotalOrders { get; init; }
    public int PreviousPeriodOrders { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal PreviousAverageOrderValue { get; init; }
    public List<DailyRevenueDto> DailyRevenue { get; init; } = new();
    public List<ChannelRevenueDto> RevenueByChannel { get; init; } = new();
    public List<PaymentMethodRevenueDto> RevenueByPaymentMethod { get; init; } = new();
}

/// <summary>
/// Daily revenue breakdown.
/// </summary>
public record DailyRevenueDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public decimal AverageOrderValue { get; init; }
}

/// <summary>
/// Revenue by sales channel.
/// </summary>
public record ChannelRevenueDto
{
    public Guid? ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public string ChannelType { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Revenue by payment method.
/// </summary>
public record PaymentMethodRevenueDto
{
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Order trends DTO with status breakdown.
/// </summary>
public record OrderTrendsDto
{
    public int TotalOrders { get; init; }
    public int PreviousPeriodOrders { get; init; }
    public decimal PercentageChange { get; init; }
    public List<DailyOrderCountDto> DailyOrders { get; init; } = new();
    public List<OrderStatusCountDto> OrdersByStatus { get; init; } = new();
    public List<HourlyOrderCountDto> OrdersByHour { get; init; } = new();
    public decimal AverageOrdersPerDay { get; init; }
    public int PeakHour { get; init; }
    public string PeakDay { get; init; } = string.Empty;
}

/// <summary>
/// Daily order count.
/// </summary>
public record DailyOrderCountDto
{
    public DateTime Date { get; init; }
    public int OrderCount { get; init; }
    public int ConfirmedCount { get; init; }
    public int CancelledCount { get; init; }
}

/// <summary>
/// Order count by status.
/// </summary>
public record OrderStatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Hourly order distribution.
/// </summary>
public record HourlyOrderCountDto
{
    public int Hour { get; init; }
    public int OrderCount { get; init; }
}

/// <summary>
/// Delivery performance metrics.
/// </summary>
public record DeliveryPerformanceDto
{
    public int TotalShipments { get; init; }
    public int DeliveredCount { get; init; }
    public int RtoCount { get; init; }
    public int InTransitCount { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal RtoRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }
    public decimal PreviousAverageDeliveryDays { get; init; }
    public List<DeliveryTimeDistributionDto> DeliveryTimeDistribution { get; init; } = new();
    public List<DailyDeliveryDto> DailyDeliveries { get; init; } = new();
    public List<StateDeliveryDto> DeliveryByState { get; init; } = new();
}

/// <summary>
/// Delivery time distribution (1-2 days, 3-5 days, etc.)
/// </summary>
public record DeliveryTimeDistributionDto
{
    public string Range { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Daily delivery stats.
/// </summary>
public record DailyDeliveryDto
{
    public DateTime Date { get; init; }
    public int DeliveredCount { get; init; }
    public int RtoCount { get; init; }
    public int NdrCount { get; init; }
}

/// <summary>
/// Delivery stats by state.
/// </summary>
public record StateDeliveryDto
{
    public string State { get; init; } = string.Empty;
    public int TotalShipments { get; init; }
    public int DeliveredCount { get; init; }
    public int RtoCount { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }
}

/// <summary>
/// Courier comparison analytics.
/// </summary>
public record CourierComparisonDto
{
    public List<CourierPerformanceDto> Couriers { get; init; } = new();
    public Guid? BestDeliveryRateCourierId { get; init; }
    public string? BestDeliveryRateCourierName { get; init; }
    public Guid? FastestDeliveryCourierId { get; init; }
    public string? FastestDeliveryCourierName { get; init; }
    public Guid? LowestRtoCourierId { get; init; }
    public string? LowestRtoCourierName { get; init; }
}

/// <summary>
/// Individual courier performance.
/// </summary>
public record CourierPerformanceDto
{
    public Guid CourierId { get; init; }
    public string CourierName { get; init; } = string.Empty;
    public string CourierType { get; init; } = string.Empty;
    public int TotalShipments { get; init; }
    public int DeliveredCount { get; init; }
    public int RtoCount { get; init; }
    public int NdrCount { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal RtoRate { get; init; }
    public decimal NdrRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }
    public decimal AverageCost { get; init; }
    public decimal TotalCost { get; init; }
}

/// <summary>
/// NDR analytics with resolution rates.
/// </summary>
public record NdrAnalyticsDto
{
    public int TotalNdrCases { get; init; }
    public int ResolvedCount { get; init; }
    public int PendingCount { get; init; }
    public int EscalatedCount { get; init; }
    public decimal ResolutionRate { get; init; }
    public decimal AverageResolutionHours { get; init; }
    public List<NdrReasonBreakdownDto> ByReason { get; init; } = new();
    public List<NdrStatusBreakdownDto> ByStatus { get; init; } = new();
    public List<DailyNdrDto> DailyNdr { get; init; } = new();
    public List<AgentPerformanceDto> AgentPerformance { get; init; } = new();
}

/// <summary>
/// NDR breakdown by reason.
/// </summary>
public record NdrReasonBreakdownDto
{
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonDescription { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Percentage { get; init; }
    public decimal ResolutionRate { get; init; }
}

/// <summary>
/// NDR breakdown by status.
/// </summary>
public record NdrStatusBreakdownDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>
/// Daily NDR stats.
/// </summary>
public record DailyNdrDto
{
    public DateTime Date { get; init; }
    public int NewCases { get; init; }
    public int ResolvedCases { get; init; }
    public int EscalatedCases { get; init; }
}

/// <summary>
/// NDR agent performance.
/// </summary>
public record AgentPerformanceDto
{
    public Guid AgentId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public int AssignedCases { get; init; }
    public int ResolvedCases { get; init; }
    public int PendingCases { get; init; }
    public decimal ResolutionRate { get; init; }
    public decimal AverageResolutionHours { get; init; }
    public int TotalCalls { get; init; }
    public int SuccessfulContacts { get; init; }
}

/// <summary>
/// Time period for analytics.
/// </summary>
public enum AnalyticsPeriod
{
    Today = 1,
    Yesterday = 2,
    Last7Days = 3,
    Last30Days = 4,
    ThisMonth = 5,
    LastMonth = 6,
    ThisQuarter = 7,
    ThisYear = 8,
    Custom = 9
}

/// <summary>
/// Helper to calculate date ranges.
/// </summary>
public static class AnalyticsPeriodHelper
{
    public static (DateTime StartDate, DateTime EndDate, DateTime PreviousStartDate, DateTime PreviousEndDate) GetDateRange(
        AnalyticsPeriod period,
        DateTime? customStartDate = null,
        DateTime? customEndDate = null)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return period switch
        {
            AnalyticsPeriod.Today => (
                today,
                now,
                today.AddDays(-1),
                today.AddSeconds(-1)),

            AnalyticsPeriod.Yesterday => (
                today.AddDays(-1),
                today.AddSeconds(-1),
                today.AddDays(-2),
                today.AddDays(-1).AddSeconds(-1)),

            AnalyticsPeriod.Last7Days => (
                today.AddDays(-7),
                now,
                today.AddDays(-14),
                today.AddDays(-7).AddSeconds(-1)),

            AnalyticsPeriod.Last30Days => (
                today.AddDays(-30),
                now,
                today.AddDays(-60),
                today.AddDays(-30).AddSeconds(-1)),

            AnalyticsPeriod.ThisMonth => (
                new DateTime(today.Year, today.Month, 1),
                now,
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1).AddSeconds(-1)),

            AnalyticsPeriod.LastMonth => (
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1).AddSeconds(-1),
                new DateTime(today.Year, today.Month, 1).AddMonths(-2),
                new DateTime(today.Year, today.Month, 1).AddMonths(-1).AddSeconds(-1)),

            AnalyticsPeriod.ThisQuarter => GetQuarterRange(today, 0),

            AnalyticsPeriod.ThisYear => (
                new DateTime(today.Year, 1, 1),
                now,
                new DateTime(today.Year - 1, 1, 1),
                new DateTime(today.Year, 1, 1).AddSeconds(-1)),

            AnalyticsPeriod.Custom when customStartDate.HasValue && customEndDate.HasValue => (
                customStartDate.Value.Date,
                customEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                customStartDate.Value.Date.AddDays(-(customEndDate.Value - customStartDate.Value).Days - 1),
                customStartDate.Value.Date.AddSeconds(-1)),

            _ => (today.AddDays(-30), now, today.AddDays(-60), today.AddDays(-30).AddSeconds(-1))
        };
    }

    private static (DateTime, DateTime, DateTime, DateTime) GetQuarterRange(DateTime today, int quarterOffset)
    {
        var currentQuarter = (today.Month - 1) / 3 + 1;
        var quarterStart = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddSeconds(-1);
        var prevQuarterStart = quarterStart.AddMonths(-3);
        var prevQuarterEnd = quarterStart.AddSeconds(-1);

        return (quarterStart, DateTime.UtcNow < quarterEnd ? DateTime.UtcNow : quarterEnd, prevQuarterStart, prevQuarterEnd);
    }
}
