using SuperEcomManager.Integrations.Couriers.Delhivery.Models;

namespace SuperEcomManager.Integrations.Couriers.Delhivery;

/// <summary>
/// HTTP client interface for Delhivery API operations.
/// </summary>
public interface IDelhiveryClient
{
    /// <summary>
    /// Creates a shipment in Delhivery.
    /// </summary>
    Task<DelhiveryCreateShipmentResponse?> CreateShipmentAsync(
        string token,
        DelhiveryCreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates waybills in advance.
    /// </summary>
    Task<DelhiveryWaybillResponse?> GenerateWaybillsAsync(
        string token,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks pincode serviceability.
    /// </summary>
    Task<DelhiveryPincodeResponse?> CheckPincodeServiceabilityAsync(
        string token,
        string pincode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for an AWB.
    /// </summary>
    Task<DelhiveryTrackingResponse?> GetTrackingAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for multiple AWBs.
    /// </summary>
    Task<DelhiveryTrackingResponse?> GetBulkTrackingAsync(
        string token,
        List<string> waybills,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a pickup.
    /// </summary>
    Task<DelhiveryPickupResponse?> SchedulePickupAsync(
        string token,
        DelhiveryPickupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a shipment.
    /// </summary>
    Task<DelhiveryCancelResponse?> CancelShipmentAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets label PDF URL for a shipment.
    /// </summary>
    Task<byte[]?> GetLabelAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default);
}
