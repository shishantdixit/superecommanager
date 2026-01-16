namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Types of events that can trigger webhooks.
/// </summary>
public enum WebhookEvent
{
    // Order events
    OrderCreated,
    OrderUpdated,
    OrderCancelled,
    OrderConfirmed,
    OrderShipped,
    OrderDelivered,

    // Shipment events
    ShipmentCreated,
    ShipmentPickedUp,
    ShipmentInTransit,
    ShipmentOutForDelivery,
    ShipmentDelivered,
    ShipmentFailed,
    ShipmentRTO,

    // NDR events
    NdrCreated,
    NdrAssigned,
    NdrResolved,
    NdrEscalated,

    // Inventory events
    InventoryLow,
    InventoryOutOfStock,
    InventoryRestocked,

    // Payment events
    PaymentReceived,
    PaymentFailed,
    RefundInitiated,
    RefundCompleted,

    // Channel events
    ChannelConnected,
    ChannelDisconnected,
    ChannelSyncCompleted
}

/// <summary>
/// Status of a webhook delivery attempt.
/// </summary>
public enum WebhookDeliveryStatus
{
    Pending,
    Delivered,
    Failed,
    Retrying
}
