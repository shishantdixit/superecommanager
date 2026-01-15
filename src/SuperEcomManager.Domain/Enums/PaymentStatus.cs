namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Payment status for orders.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment not yet received</summary>
    Pending = 0,

    /// <summary>Payment received and confirmed</summary>
    Paid = 1,

    /// <summary>Payment partially received</summary>
    PartiallyPaid = 2,

    /// <summary>Payment failed</summary>
    Failed = 3,

    /// <summary>Payment refunded</summary>
    Refunded = 4,

    /// <summary>Payment partially refunded</summary>
    PartiallyRefunded = 5,

    /// <summary>Cash on Delivery - payment pending delivery</summary>
    CODPending = 6,

    /// <summary>COD payment collected</summary>
    CODCollected = 7
}
