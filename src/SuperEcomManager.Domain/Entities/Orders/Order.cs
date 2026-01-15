using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.Events.Orders;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Orders;

/// <summary>
/// Represents a unified order from any sales channel.
/// This is the core business entity that maps all platform-specific orders
/// to a common internal model.
/// </summary>
public class Order : AuditableEntity, ISoftDeletable
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid ChannelId { get; private set; }
    public string ExternalOrderId { get; private set; } = string.Empty;
    public string? ExternalOrderNumber { get; private set; }

    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public FulfillmentStatus FulfillmentStatus { get; private set; }

    // Customer Information
    public string CustomerName { get; private set; } = string.Empty;
    public string? CustomerEmail { get; private set; }
    public string? CustomerPhone { get; private set; }

    // Addresses stored as JSON in database
    public Address ShippingAddress { get; private set; } = null!;
    public Address? BillingAddress { get; private set; }

    // Financial Information
    public Money Subtotal { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public Money TaxAmount { get; private set; } = Money.Zero;
    public Money ShippingAmount { get; private set; } = Money.Zero;
    public Money TotalAmount { get; private set; } = Money.Zero;

    public PaymentMethod? PaymentMethod { get; private set; }
    public bool IsCOD => PaymentMethod == Enums.PaymentMethod.COD;

    // Dates
    public DateTime OrderDate { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Notes
    public string? CustomerNotes { get; private set; }
    public string? InternalNotes { get; private set; }

    // Platform-specific data stored as JSON
    public string? PlatformData { get; private set; }

    // Soft delete
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public SalesChannel? Channel { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Order() { } // EF Core constructor

    public static Order Create(
        Guid channelId,
        string externalOrderId,
        string customerName,
        Address shippingAddress,
        Money totalAmount,
        DateTime orderDate,
        string? externalOrderNumber = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            ChannelId = channelId,
            ExternalOrderId = externalOrderId,
            ExternalOrderNumber = externalOrderNumber,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            FulfillmentStatus = FulfillmentStatus.Unfulfilled,
            CustomerName = customerName,
            ShippingAddress = shippingAddress,
            TotalAmount = totalAmount,
            Subtotal = totalAmount,
            OrderDate = orderDate,
            CreatedAt = DateTime.UtcNow
        };

        order._statusHistory.Add(new OrderStatusHistory(order.Id, OrderStatus.Pending, null, "Order created"));
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.OrderNumber, channelId));

        return order;
    }

    public void SetCustomerInfo(string? email, string? phone)
    {
        CustomerEmail = email;
        CustomerPhone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBillingAddress(Address? billingAddress)
    {
        BillingAddress = billingAddress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaymentInfo(PaymentMethod paymentMethod, PaymentStatus paymentStatus)
    {
        PaymentMethod = paymentMethod;
        PaymentStatus = paymentStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNotes(string? customerNotes, string? internalNotes)
    {
        CustomerNotes = customerNotes;
        InternalNotes = internalNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlatformData(string? platformData)
    {
        PlatformData = platformData;
    }

    public void UpdateStatus(OrderStatus newStatus, Guid? userId = null, string? reason = null)
    {
        if (Status == newStatus) return;

        ValidateStatusTransition(newStatus);

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        // Update related timestamps
        switch (newStatus)
        {
            case OrderStatus.Shipped:
                ShippedAt = DateTime.UtcNow;
                FulfillmentStatus = FulfillmentStatus.Fulfilled;
                break;
            case OrderStatus.Delivered:
                DeliveredAt = DateTime.UtcNow;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = DateTime.UtcNow;
                break;
        }

        _statusHistory.Add(new OrderStatusHistory(Id, newStatus, userId, reason));
        AddDomainEvent(new OrderStatusChangedEvent(Id, oldStatus, newStatus, reason));
    }

    public void UpdatePaymentStatus(PaymentStatus newStatus)
    {
        PaymentStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        RecalculateTotals();
    }

    public void SetFinancials(Money subtotal, Money discountAmount, Money taxAmount, Money shippingAmount)
    {
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        ShippingAmount = shippingAmount;
        RecalculateTotals();
    }

    public void Cancel(Guid? userId = null, string? reason = null)
    {
        if (Status == OrderStatus.Cancelled)
            return;

        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered order");

        UpdateStatus(OrderStatus.Cancelled, userId, reason ?? "Order cancelled");
        AddDomainEvent(new OrderCancelledEvent(Id, reason));
    }

    private void RecalculateTotals()
    {
        if (_items.Any())
        {
            Subtotal = new Money(_items.Sum(i => i.TotalAmount.Amount), TotalAmount.Currency);
        }

        TotalAmount = Subtotal - DiscountAmount + TaxAmount + ShippingAmount;
    }

    private void ValidateStatusTransition(OrderStatus newStatus)
    {
        var invalidTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Cancelled, new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing } },
            { OrderStatus.Delivered, new[] { OrderStatus.Pending, OrderStatus.Cancelled } },
        };

        if (Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot change status of a cancelled order");

        if (Status == OrderStatus.Delivered && newStatus != OrderStatus.Returned && newStatus != OrderStatus.RTO)
            throw new InvalidOperationException("Delivered order can only transition to Returned or RTO");
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"ORD-{timestamp}-{random}";
    }
}
