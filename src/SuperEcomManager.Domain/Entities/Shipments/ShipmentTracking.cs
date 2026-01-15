using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Shipments;

/// <summary>
/// Represents a tracking event for a shipment.
/// </summary>
public class ShipmentTracking : BaseEntity
{
    public Guid ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public string? Location { get; private set; }
    public string? Remarks { get; private set; }
    public DateTime EventTime { get; private set; }

    public Shipment? Shipment { get; private set; }

    private ShipmentTracking() { }

    public ShipmentTracking(Guid shipmentId, ShipmentStatus status, string? location = null, string? remarks = null)
    {
        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        Status = status;
        Location = location;
        Remarks = remarks;
        EventTime = DateTime.UtcNow;
    }
}
