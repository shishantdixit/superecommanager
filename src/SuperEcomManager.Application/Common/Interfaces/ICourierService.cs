using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Shipments;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Entities.Shipments;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for interacting with courier APIs.
/// </summary>
public interface ICourierService
{
    /// <summary>
    /// Creates a shipment with the configured courier and retrieves AWB details.
    /// </summary>
    Task<CourierShipmentResult> CreateShipmentAsync(
        Guid orderId,
        CourierType courierType,
        Guid? courierAccountId = null,
        string? serviceCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a shipment with the configured courier using provided shipment and order entities.
    /// This method does NOT query the database and should be called BEFORE saving the shipment.
    /// </summary>
    Task<CourierShipmentResult> CreateShipmentAsync(
        Shipment shipment,
        Order order,
        Guid? courierAccountId = null,
        string? serviceCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connection to a courier account by validating credentials with the courier API.
    /// </summary>
    Task<CourierConnectionResult> TestConnectionAsync(
        Guid courierAccountId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of courier connection test.
/// </summary>
public record CourierConnectionResult
{
    public bool IsConnected { get; init; }
    public string? Message { get; init; }
    public string? AccountName { get; init; }

    public static CourierConnectionResult Connected(string? accountName = null) =>
        new() { IsConnected = true, Message = "Connection successful", AccountName = accountName };

    public static CourierConnectionResult Failed(string message, string? accountName = null) =>
        new() { IsConnected = false, Message = message, AccountName = accountName };
}

/// <summary>
/// Result of courier shipment creation.
/// </summary>
public record CourierShipmentResult
{
    public bool Success { get; init; }
    public string? AwbNumber { get; init; }
    public string? CourierName { get; init; }
    public string? TrackingUrl { get; init; }
    public string? LabelUrl { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// External order ID from the courier system (e.g., Shiprocket order_id).
    /// </summary>
    public string? ExternalOrderId { get; init; }

    /// <summary>
    /// External shipment ID from the courier system (e.g., Shiprocket shipment_id).
    /// </summary>
    public string? ExternalShipmentId { get; init; }

    /// <summary>
    /// Indicates partial success - order was created in courier system but AWB assignment failed.
    /// When true, ExternalOrderId and ExternalShipmentId are populated but AwbNumber is empty.
    /// </summary>
    public bool IsPartialSuccess { get; init; }

    public static CourierShipmentResult Failure(string error) =>
        new() { Success = false, ErrorMessage = error };

    public static CourierShipmentResult Ok(
        string awbNumber,
        string? courierName = null,
        string? trackingUrl = null,
        string? labelUrl = null,
        string? externalOrderId = null,
        string? externalShipmentId = null) =>
        new()
        {
            Success = true,
            AwbNumber = awbNumber,
            CourierName = courierName,
            TrackingUrl = trackingUrl,
            LabelUrl = labelUrl,
            ExternalOrderId = externalOrderId,
            ExternalShipmentId = externalShipmentId
        };

    /// <summary>
    /// Creates a partial success result - order created but AWB assignment failed.
    /// </summary>
    public static CourierShipmentResult PartialSuccess(
        string externalOrderId,
        string? externalShipmentId,
        string awbError) =>
        new()
        {
            Success = true,
            IsPartialSuccess = true,
            ExternalOrderId = externalOrderId,
            ExternalShipmentId = externalShipmentId,
            ErrorMessage = awbError
        };
}
