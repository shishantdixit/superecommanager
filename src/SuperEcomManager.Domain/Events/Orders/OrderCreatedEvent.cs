using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Events.Orders;

/// <summary>
/// Event raised when a new order is created.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public Guid ChannelId { get; }

    public OrderCreatedEvent(Guid orderId, string orderNumber, Guid channelId)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        ChannelId = channelId;
    }
}
