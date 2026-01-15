using SuperEcomManager.Integrations.Couriers.DTDC.Models;

namespace SuperEcomManager.Integrations.Couriers.DTDC;

/// <summary>
/// HTTP client interface for DTDC API operations.
/// </summary>
public interface IDTDCClient
{
    /// <summary>
    /// Creates a shipment in DTDC.
    /// </summary>
    Task<DTDCCreateShipmentResponse?> CreateShipmentAsync(
        string apiKey,
        DTDCCreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks pincode serviceability.
    /// </summary>
    Task<DTDCPincodeResponse?> CheckPincodeServiceabilityAsync(
        string apiKey,
        string pincode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for a consignment.
    /// </summary>
    Task<DTDCTrackingResponse?> GetTrackingAsync(
        string apiKey,
        string consignmentNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a pickup.
    /// </summary>
    Task<DTDCPickupResponse?> SchedulePickupAsync(
        string apiKey,
        DTDCPickupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a consignment.
    /// </summary>
    Task<DTDCCancelResponse?> CancelShipmentAsync(
        string apiKey,
        DTDCCancelRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipping rates.
    /// </summary>
    Task<DTDCRateResponse?> GetRatesAsync(
        string apiKey,
        DTDCRateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets label PDF for a shipment.
    /// </summary>
    Task<byte[]?> GetLabelAsync(
        string apiKey,
        string consignmentNumber,
        CancellationToken cancellationToken = default);
}
