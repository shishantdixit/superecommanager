using SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// HTTP client interface for Shiprocket API.
/// </summary>
public interface IShiprocketClient
{
    /// <summary>
    /// Authenticates with Shiprocket and gets access token.
    /// </summary>
    Task<ShiprocketAuthResponse?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an order in Shiprocket.
    /// </summary>
    Task<ShiprocketCreateOrderResponse?> CreateOrderAsync(
        string token,
        ShiprocketCreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates AWB for a shipment.
    /// </summary>
    Task<ShiprocketAwbResponse?> GenerateAwbAsync(
        string token,
        ShiprocketGenerateAwbRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets courier serviceability and rates.
    /// </summary>
    Task<ShiprocketServiceabilityResponse?> CheckServiceabilityAsync(
        string token,
        string pickupPincode,
        string deliveryPincode,
        decimal weight,
        bool isCod,
        long? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for an AWB.
    /// </summary>
    Task<ShiprocketTrackingResponse?> GetTrackingAsync(
        string token,
        string awbCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules pickup for shipments.
    /// </summary>
    Task<ShiprocketPickupResponse?> SchedulePickupAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a shipment.
    /// </summary>
    Task<ShiprocketCancelResponse?> CancelShipmentAsync(
        string token,
        List<long> orderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipping label URL.
    /// </summary>
    Task<ShiprocketLabelResponse?> GetLabelAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads label PDF as bytes.
    /// </summary>
    Task<byte[]?> DownloadLabelAsync(
        string labelUrl,
        CancellationToken cancellationToken = default);
}
