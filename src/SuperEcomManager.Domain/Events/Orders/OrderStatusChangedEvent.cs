using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Events.Orders;

/// <summary>
/// Event raised when order status changes.
/// </summary>
public class OrderStatusChangedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public OrderStatus OldStatus { get; }
    public OrderStatus NewStatus { get; }
    public string? Reason { get; }

    public OrderStatusChangedEvent(Guid orderId, OrderStatus oldStatus, OrderStatus newStatus, string? reason = null)
    {
        OrderId = orderId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
    }
}
