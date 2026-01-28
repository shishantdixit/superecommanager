using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Integrations.Couriers;

/// <summary>
/// Interface for courier/shipping provider adapters.
/// Each courier integration (Shiprocket, Delhivery, etc.) implements this interface.
/// </summary>
public interface ICourierAdapter
{
    /// <summary>
    /// The courier type this adapter handles.
    /// </summary>
    CourierType CourierType { get; }

    /// <summary>
    /// Display name of the courier.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Validates the provided credentials by making a test API call.
    /// </summary>
    Task<CourierResult> ValidateCredentialsAsync(
        CourierCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available serviceability/rates for a shipment.
    /// </summary>
    Task<CourierResult<List<CourierRate>>> GetRatesAsync(
        CourierCredentials credentials,
        RateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a shipment and gets AWB number.
    /// </summary>
    Task<CourierResult<ShipmentResponse>> CreateShipmentAsync(
        CourierCredentials credentials,
        ShipmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for a shipment.
    /// </summary>
    Task<CourierResult<TrackingResponse>> GetTrackingAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a shipment.
    /// </summary>
    Task<CourierResult> CancelShipmentAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates shipping label PDF.
    /// </summary>
    Task<CourierResult<byte[]>> GetLabelAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules pickup for shipments.
    /// </summary>
    Task<CourierResult<PickupResponse>> SchedulePickupAsync(
        CourierCredentials credentials,
        PickupRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Courier API credentials.
/// </summary>
public class CourierCredentials
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountId { get; set; }
    public string? ChannelId { get; set; }
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}

/// <summary>
/// Generic result wrapper for courier operations.
/// </summary>
public class CourierResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static CourierResult Success() => new() { IsSuccess = true };
    public static CourierResult Failure(string message, string? code = null) =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
}

/// <summary>
/// Generic result wrapper with data.
/// </summary>
public class CourierResult<T> : CourierResult
{
    public T? Data { get; set; }

    public static CourierResult<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public new static CourierResult<T> Failure(string message, string? code = null) =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
}

/// <summary>
/// Request for shipping rate calculation.
/// </summary>
public class RateRequest
{
    public string PickupPincode { get; set; } = string.Empty;
    public string DeliveryPincode { get; set; } = string.Empty;
    public decimal Weight { get; set; } // in kg
    public decimal? Length { get; set; } // in cm
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? DeclaredValue { get; set; }
    public bool IsCOD { get; set; }
    public decimal? CODAmount { get; set; }
}

/// <summary>
/// Shipping rate from a courier.
/// </summary>
public class CourierRate
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal FreightCharge { get; set; }
    public decimal CODCharge { get; set; }
    public decimal TotalCharge { get; set; }
    public int EstimatedDays { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public bool IsExpress { get; set; }
    public bool IsSurface { get; set; }
}

/// <summary>
/// Request to create a shipment.
/// </summary>
public class ShipmentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;

    // Pickup
    public string PickupName { get; set; } = string.Empty;
    public string PickupPhone { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;
    public string PickupPincode { get; set; } = string.Empty;

    // Delivery
    public string DeliveryName { get; set; } = string.Empty;
    public string DeliveryPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryPincode { get; set; } = string.Empty;

    // Package
    public decimal Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }

    // Payment
    public bool IsCOD { get; set; }
    public decimal? CODAmount { get; set; }
    public decimal DeclaredValue { get; set; }

    // Items
    public List<ShipmentItemRequest> Items { get; set; } = new();

    // Service
    public string? ServiceCode { get; set; }
    public bool IsExpress { get; set; }
}

public class ShipmentItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Response from shipment creation.
/// </summary>
public class ShipmentResponse
{
    public string AwbNumber { get; set; } = string.Empty;
    public string? CourierName { get; set; }
    public string? ShipmentId { get; set; }
    public string? LabelUrl { get; set; }
    public string? TrackingUrl { get; set; }
    public decimal? FreightCharge { get; set; }
    public DateTime? ExpectedDelivery { get; set; }

    /// <summary>
    /// External order ID from the courier system (e.g., Shiprocket order_id).
    /// </summary>
    public string? ExternalOrderId { get; set; }

    /// <summary>
    /// External shipment ID from the courier system (e.g., Shiprocket shipment_id).
    /// </summary>
    public string? ExternalShipmentId { get; set; }

    /// <summary>
    /// Indicates partial success - order was created in courier system but AWB assignment failed.
    /// </summary>
    public bool IsPartialSuccess { get; set; }

    /// <summary>
    /// Error message for AWB assignment failure (only set when IsPartialSuccess is true).
    /// </summary>
    public string? AwbError { get; set; }
}

/// <summary>
/// Tracking information response.
/// </summary>
public class TrackingResponse
{
    public string AwbNumber { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string? CurrentLocation { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveredTo { get; set; }
    public List<TrackingEvent> Events { get; set; } = new();
}

public class TrackingEvent
{
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// Request to schedule pickup.
/// </summary>
public class PickupRequest
{
    public List<string> AwbNumbers { get; set; } = new();
    public DateTime PickupDate { get; set; }
    public string? PickupTimeSlot { get; set; }
    public string? WarehouseId { get; set; }
}

/// <summary>
/// Response from pickup scheduling.
/// </summary>
public class PickupResponse
{
    public string? PickupId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? TimeSlot { get; set; }
    public int ShipmentCount { get; set; }
}
