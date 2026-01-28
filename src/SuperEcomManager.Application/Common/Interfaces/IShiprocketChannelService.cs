namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// DTO for Shiprocket channel information.
/// </summary>
public record ShiprocketChannelDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
}

/// <summary>
/// DTO for Shiprocket pickup location information.
/// </summary>
public record ShiprocketPickupLocationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PinCode { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Result of serviceability check containing available couriers.
/// </summary>
public record ServiceabilityResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? RecommendedCourierId { get; init; }
    public List<AvailableCourierInfo>? AvailableCouriers { get; init; }
}

/// <summary>
/// Information about an available courier from serviceability check.
/// </summary>
public record AvailableCourierInfo
{
    public int CourierId { get; init; }
    public string CourierName { get; init; } = string.Empty;
    public decimal FreightCharge { get; init; }
    public decimal CodCharges { get; init; }
    public string? EstimatedDeliveryDays { get; init; }
    public string? Etd { get; init; }
    public decimal Rating { get; init; }
    public bool IsSurface { get; init; }
}

/// <summary>
/// Result of AWB generation.
/// </summary>
public record AwbGenerationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? AwbCode { get; init; }
    public string? CourierName { get; init; }
    public int? CourierCompanyId { get; init; }
    public string? LabelUrl { get; init; }
    public string? TrackingUrl { get; init; }
}

/// <summary>
/// Service for fetching Shiprocket account configuration like channels and pickup locations.
/// </summary>
public interface IShiprocketChannelService
{
    /// <summary>
    /// Gets the list of channels from Shiprocket for the given API credentials.
    /// </summary>
    Task<List<ShiprocketChannelDto>> GetChannelsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of pickup locations from Shiprocket for the given API credentials.
    /// </summary>
    Task<List<ShiprocketPickupLocationDto>> GetPickupLocationsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks courier serviceability for a given route.
    /// </summary>
    Task<ServiceabilityResult> CheckServiceabilityAsync(
        Guid courierAccountId,
        string pickupPincode,
        string deliveryPincode,
        decimal weight,
        bool isCod,
        long? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates AWB for a shipment by assigning a courier.
    /// </summary>
    Task<AwbGenerationResult> GenerateAwbAsync(
        Guid courierAccountId,
        long shipmentId,
        int? courierId = null,
        CancellationToken cancellationToken = default);
}
