namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Fulfillment status for orders.
/// </summary>
public enum FulfillmentStatus
{
    /// <summary>Not yet fulfilled</summary>
    Unfulfilled = 0,

    /// <summary>Partially fulfilled (some items shipped)</summary>
    PartiallyFulfilled = 1,

    /// <summary>Fully fulfilled (all items shipped)</summary>
    Fulfilled = 2,

    /// <summary>Fulfillment on hold</summary>
    OnHold = 3
}
