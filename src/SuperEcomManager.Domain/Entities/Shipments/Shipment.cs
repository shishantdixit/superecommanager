using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Shipments;

/// <summary>
/// Represents a shipment for an order.
/// </summary>
public class Shipment : AuditableEntity, ISoftDeletable
{
    public Guid OrderId { get; private set; }
    public string ShipmentNumber { get; private set; } = string.Empty;
    public string? AwbNumber { get; private set; }
    public CourierType CourierType { get; private set; }
    public string? CourierName { get; private set; }
    public ShipmentStatus Status { get; private set; }

    public Address PickupAddress { get; private set; } = null!;
    public Address DeliveryAddress { get; private set; } = null!;

    public Dimensions? Dimensions { get; private set; }
    public Money? ShippingCost { get; private set; }
    public Money? CODAmount { get; private set; }
    public bool IsCOD { get; private set; }

    public DateTime? PickedUpAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }

    public string? LabelUrl { get; private set; }
    public string? TrackingUrl { get; private set; }
    public string? CourierResponse { get; private set; }

    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private readonly List<ShipmentItem> _items = new();
    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();

    private readonly List<ShipmentTracking> _trackingEvents = new();
    public IReadOnlyCollection<ShipmentTracking> TrackingEvents => _trackingEvents.AsReadOnly();

    private Shipment() { }

    public static Shipment Create(
        Guid orderId,
        Address pickupAddress,
        Address deliveryAddress,
        CourierType courierType,
        bool isCOD = false,
        Money? codAmount = null)
    {
        return new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ShipmentNumber = GenerateShipmentNumber(),
            PickupAddress = pickupAddress,
            DeliveryAddress = deliveryAddress,
            CourierType = courierType,
            IsCOD = isCOD,
            CODAmount = codAmount,
            Status = ShipmentStatus.Created,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAwb(string awbNumber, string? courierName, string? labelUrl, string? trackingUrl)
    {
        AwbNumber = awbNumber;
        CourierName = courierName;
        LabelUrl = labelUrl;
        TrackingUrl = trackingUrl;
        Status = ShipmentStatus.Manifested;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(ShipmentStatus newStatus, string? location = null, string? remarks = null)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        _trackingEvents.Add(new ShipmentTracking(Id, newStatus, location, remarks));

        if (newStatus == ShipmentStatus.PickedUp)
            PickedUpAt = DateTime.UtcNow;
        if (newStatus == ShipmentStatus.Delivered)
            DeliveredAt = DateTime.UtcNow;
    }

    public void AddItem(ShipmentItem item) => _items.Add(item);

    private static string GenerateShipmentNumber()
    {
        return $"SHP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}
