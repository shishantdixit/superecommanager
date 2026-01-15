using SuperEcomManager.Integrations.Couriers.BlueDart.Models;

namespace SuperEcomManager.Integrations.Couriers.BlueDart;

/// <summary>
/// HTTP client interface for BlueDart API operations.
/// </summary>
public interface IBlueDartClient
{
    /// <summary>
    /// Generates a waybill/shipment.
    /// </summary>
    Task<BlueDartWaybillResponse?> GenerateWaybillAsync(
        BlueDartProfile profile,
        BlueDartWaybillRequestData request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks pincode serviceability.
    /// </summary>
    Task<BlueDartPincodeResponse?> CheckPincodeServiceabilityAsync(
        BlueDartProfile profile,
        string pincode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for an AWB.
    /// </summary>
    Task<BlueDartTrackingResponse?> GetTrackingAsync(
        BlueDartProfile profile,
        string awbNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a pickup.
    /// </summary>
    Task<BlueDartPickupResponse?> SchedulePickupAsync(
        BlueDartProfile profile,
        BlueDartPickupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a waybill.
    /// </summary>
    Task<BlueDartCancelResponse?> CancelWaybillAsync(
        BlueDartProfile profile,
        string awbNumber,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets label PDF for a shipment.
    /// </summary>
    Task<byte[]?> GetLabelAsync(
        BlueDartProfile profile,
        string awbNumber,
        CancellationToken cancellationToken = default);
}
