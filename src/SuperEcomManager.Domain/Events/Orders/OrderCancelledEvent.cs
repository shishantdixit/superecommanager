using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Events.Orders;

/// <summary>
/// Event raised when an order is cancelled.
/// </summary>
public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string? Reason { get; }

    public OrderCancelledEvent(Guid orderId, string? reason = null)
    {
        OrderId = orderId;
        Reason = reason;
    }
}
