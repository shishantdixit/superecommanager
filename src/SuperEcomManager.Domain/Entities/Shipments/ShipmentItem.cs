using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Shipments;

/// <summary>
/// Represents an item in a shipment.
/// </summary>
public class ShipmentItem : BaseEntity
{
    public Guid ShipmentId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    public Shipment? Shipment { get; private set; }

    private ShipmentItem() { }

    public ShipmentItem(Guid shipmentId, Guid orderItemId, string sku, string name, int quantity)
    {
        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        OrderItemId = orderItemId;
        Sku = sku;
        Name = name;
        Quantity = quantity;
    }
}
