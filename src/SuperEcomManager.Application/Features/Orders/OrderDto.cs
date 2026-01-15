using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Data transfer object for Order.
/// </summary>
public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid ChannelId { get; init; }
    public string? ChannelName { get; init; }
    public string ExternalOrderId { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public FulfillmentStatus FulfillmentStatus { get; init; }
    public AddressDto ShippingAddress { get; init; } = null!;
    public AddressDto? BillingAddress { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public MoneyDto TotalAmount { get; init; } = null!;
    public MoneyDto Subtotal { get; init; } = null!;
    public MoneyDto ShippingAmount { get; init; } = null!;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto TaxAmount { get; init; } = null!;
    public PaymentMethod? PaymentMethod { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = new List<OrderItemDto>();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for OrderItem.
/// </summary>
public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid? ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
    public MoneyDto TotalAmount { get; init; } = null!;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto TaxAmount { get; init; } = null!;
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
}

/// <summary>
/// Data transfer object for Money value object.
/// </summary>
public record MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
}

/// <summary>
/// Data transfer object for Address value object.
/// </summary>
public record AddressDto
{
    public string Name { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
}

/// <summary>
/// DTO for order list item (lightweight).
/// </summary>
public record OrderListDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? ExternalOrderNumber { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public ChannelType ChannelType { get; init; }
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public FulfillmentStatus FulfillmentStatus { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingState { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public bool IsCOD { get; init; }
    public int ItemCount { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for order status history entry.
/// </summary>
public record OrderStatusHistoryDto
{
    public OrderStatus Status { get; init; }
    public string? Reason { get; init; }
    public DateTime ChangedAt { get; init; }
    public Guid? ChangedBy { get; init; }
}

/// <summary>
/// Full DTO for order details.
/// </summary>
public record OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ExternalOrderId { get; init; } = string.Empty;
    public string? ExternalOrderNumber { get; init; }

    // Channel Info
    public Guid ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public ChannelType ChannelType { get; init; }

    // Status
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public FulfillmentStatus FulfillmentStatus { get; init; }

    // Customer
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }

    // Addresses
    public AddressDto ShippingAddress { get; init; } = null!;
    public AddressDto? BillingAddress { get; init; }

    // Financials
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";

    // Payment
    public PaymentMethod? PaymentMethod { get; init; }
    public bool IsCOD { get; init; }

    // Dates
    public DateTime OrderDate { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Notes
    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }

    // Items
    public List<OrderItemDto> Items { get; init; } = new();

    // Status History
    public List<OrderStatusHistoryDto> StatusHistory { get; init; } = new();

    // Shipment Info (if any)
    public ShipmentSummaryDto? Shipment { get; init; }
}

/// <summary>
/// Summary DTO for shipment attached to order.
/// </summary>
public record ShipmentSummaryDto
{
    public Guid Id { get; init; }
    public string AwbNumber { get; init; } = string.Empty;
    public string CourierName { get; init; } = string.Empty;
    public CourierType CourierType { get; init; }
    public ShipmentStatus Status { get; init; }
    public string? TrackingUrl { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
}

/// <summary>
/// Filter parameters for orders query.
/// </summary>
public record OrderFilterDto
{
    public string? SearchTerm { get; init; }
    public OrderStatus? Status { get; init; }
    public List<OrderStatus>? Statuses { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public FulfillmentStatus? FulfillmentStatus { get; init; }
    public Guid? ChannelId { get; init; }
    public ChannelType? ChannelType { get; init; }
    public bool? IsCOD { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
}

/// <summary>
/// Sort options for orders.
/// </summary>
public enum OrderSortBy
{
    OrderDate,
    CreatedAt,
    TotalAmount,
    CustomerName,
    Status
}

/// <summary>
/// Order statistics DTO.
/// </summary>
public record OrderStatsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int ConfirmedOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int ShippedOrders { get; init; }
    public int DeliveredOrders { get; init; }
    public int CancelledOrders { get; init; }
    public int RTOOrders { get; init; }
    public int ReturnedOrders { get; init; }

    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int TotalCODOrders { get; init; }
    public decimal CODAmount { get; init; }

    public Dictionary<string, int> OrdersByChannel { get; init; } = new();
    public Dictionary<string, decimal> RevenueByChannel { get; init; } = new();
    public List<DailyOrderStat> DailyStats { get; init; } = new();
}

/// <summary>
/// Daily order statistics.
/// </summary>
public record DailyOrderStat
{
    public DateTime Date { get; init; }
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
}
