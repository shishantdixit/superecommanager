using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Data transfer object for Shipment.
/// </summary>
public record ShipmentDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string ShipmentNumber { get; init; } = string.Empty;
    public string? AwbNumber { get; init; }
    public CourierType CourierType { get; init; }
    public string? CourierName { get; init; }
    public ShipmentStatus Status { get; init; }
    public AddressDto PickupAddress { get; init; } = null!;
    public AddressDto DeliveryAddress { get; init; } = null!;
    public DimensionsDto? Dimensions { get; init; }
    public decimal? ShippingCost { get; init; }
    public string? ShippingCostCurrency { get; init; }
    public decimal? CODAmount { get; init; }
    public string? CODCurrency { get; init; }
    public bool IsCOD { get; init; }
    public DateTime? PickedUpAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? LabelUrl { get; init; }
    public string? TrackingUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Address DTO for shipment addresses.
/// </summary>
public record AddressDto
{
    public string Name { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
}

/// <summary>
/// Dimensions DTO for package dimensions.
/// </summary>
public record DimensionsDto
{
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public decimal Weight { get; init; }
}

/// <summary>
/// Lightweight DTO for shipment list view.
/// </summary>
public record ShipmentListDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ShipmentNumber { get; init; } = string.Empty;
    public string? AwbNumber { get; init; }
    public CourierType CourierType { get; init; }
    public string? CourierName { get; init; }
    public ShipmentStatus Status { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public bool IsCOD { get; init; }
    public decimal? CODAmount { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Full DTO for shipment details.
/// </summary>
public record ShipmentDetailDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ShipmentNumber { get; init; } = string.Empty;
    public string? AwbNumber { get; init; }
    public CourierType CourierType { get; init; }
    public string? CourierName { get; init; }
    public ShipmentStatus Status { get; init; }
    public AddressDto PickupAddress { get; init; } = null!;
    public AddressDto DeliveryAddress { get; init; } = null!;
    public DimensionsDto? Dimensions { get; init; }
    public decimal? ShippingCost { get; init; }
    public string? ShippingCostCurrency { get; init; }
    public decimal? CODAmount { get; init; }
    public string? CODCurrency { get; init; }
    public bool IsCOD { get; init; }
    public DateTime? PickedUpAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? LabelUrl { get; init; }
    public string? TrackingUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Order Info
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }

    // Items
    public List<ShipmentItemDto> Items { get; init; } = new();

    // Tracking History
    public List<ShipmentTrackingDto> TrackingHistory { get; init; } = new();
}

/// <summary>
/// DTO for shipment item.
/// </summary>
public record ShipmentItemDto
{
    public Guid Id { get; init; }
    public Guid OrderItemId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

/// <summary>
/// DTO for shipment tracking event.
/// </summary>
public record ShipmentTrackingDto
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
    public string? Location { get; init; }
    public string? Remarks { get; init; }
    public DateTime EventTime { get; init; }
}

/// <summary>
/// Filter parameters for shipments query.
/// </summary>
public record ShipmentFilterDto
{
    public string? SearchTerm { get; init; }
    public Guid? OrderId { get; init; }
    public ShipmentStatus? Status { get; init; }
    public List<ShipmentStatus>? Statuses { get; init; }
    public CourierType? CourierType { get; init; }
    public bool? IsCOD { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
}

/// <summary>
/// Sort options for shipments.
/// </summary>
public enum ShipmentSortBy
{
    CreatedAt,
    ExpectedDeliveryDate,
    Status,
    CourierType
}

/// <summary>
/// DTO for tracking info from courier.
/// </summary>
public record TrackingInfoDto
{
    public string AwbNumber { get; init; } = string.Empty;
    public string? CourierName { get; init; }
    public ShipmentStatus CurrentStatus { get; init; }
    public string? CurrentLocation { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public List<TrackingEventDto> Events { get; init; } = new();
}

/// <summary>
/// DTO for a tracking event from courier.
/// </summary>
public record TrackingEventDto
{
    public DateTime EventTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>
/// Shipment statistics DTO.
/// </summary>
public record ShipmentStatsDto
{
    public int TotalShipments { get; init; }
    public int CreatedCount { get; init; }
    public int ManifestedCount { get; init; }
    public int PickedUpCount { get; init; }
    public int InTransitCount { get; init; }
    public int OutForDeliveryCount { get; init; }
    public int DeliveredCount { get; init; }
    public int DeliveryFailedCount { get; init; }
    public int RTOCount { get; init; }
    public int CancelledCount { get; init; }
    public int LostCount { get; init; }

    public Dictionary<string, int> ShipmentsByCourier { get; init; } = new();
    public decimal DeliverySuccessRate { get; init; }
    public decimal AverageDeliveryDays { get; init; }
}
