using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Orders;

/// <summary>
/// Records order status changes for audit trail.
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Order? Order { get; private set; }

    private OrderStatusHistory() { } // EF Core constructor

    public OrderStatusHistory(Guid orderId, OrderStatus status, Guid? changedBy, string? reason = null)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Status = status;
        ChangedBy = changedBy;
        Reason = reason;
        ChangedAt = DateTime.UtcNow;
    }
}
