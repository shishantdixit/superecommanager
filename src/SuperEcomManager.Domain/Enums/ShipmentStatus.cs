namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Shipment status representing the shipment lifecycle.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>Shipment created but not yet picked up</summary>
    Created = 0,

    /// <summary>Manifest generated</summary>
    Manifested = 1,

    /// <summary>Shipment picked up by courier</summary>
    PickedUp = 2,

    /// <summary>Shipment in transit</summary>
    InTransit = 3,

    /// <summary>Shipment reached destination hub</summary>
    ReachedDestination = 4,

    /// <summary>Out for delivery</summary>
    OutForDelivery = 5,

    /// <summary>Delivered successfully</summary>
    Delivered = 6,

    /// <summary>Delivery attempted but failed (NDR)</summary>
    DeliveryFailed = 7,

    /// <summary>Shipment marked for return (RTO initiated)</summary>
    RTOInitiated = 8,

    /// <summary>RTO shipment in transit back to origin</summary>
    RTOInTransit = 9,

    /// <summary>RTO delivered back to seller</summary>
    RTODelivered = 10,

    /// <summary>Shipment cancelled</summary>
    Cancelled = 11,

    /// <summary>Shipment lost</summary>
    Lost = 12
}
