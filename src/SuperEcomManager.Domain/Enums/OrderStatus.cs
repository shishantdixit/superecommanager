namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Order status representing the lifecycle of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order received but not yet processed</summary>
    Pending = 0,

    /// <summary>Order confirmed and ready for fulfillment</summary>
    Confirmed = 1,

    /// <summary>Order is being processed/picked</summary>
    Processing = 2,

    /// <summary>Order shipped to customer</summary>
    Shipped = 3,

    /// <summary>Order delivered to customer</summary>
    Delivered = 4,

    /// <summary>Order cancelled by customer or seller</summary>
    Cancelled = 5,

    /// <summary>Order returned by customer</summary>
    Returned = 6,

    /// <summary>Return to origin (RTO)</summary>
    RTO = 7,

    /// <summary>Order on hold (payment pending, verification required, etc.)</summary>
    OnHold = 8,

    /// <summary>Order failed (payment failure, verification failure, etc.)</summary>
    Failed = 9
}
