using System.Text.Json;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.DTDC.Models;

namespace SuperEcomManager.Integrations.Couriers.DTDC;

/// <summary>
/// Handler for DTDC webhooks.
/// </summary>
public interface IDTDCWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload from DTDC.
    /// </summary>
    Task<DTDCWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of DTDC webhook processing.
/// </summary>
public class DTDCWebhookResult
{
    public bool Success { get; set; }
    public string? AwbNumber { get; set; }
    public string? OrderId { get; set; }
    public ShipmentStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public DTDCWebhookPayload? Payload { get; set; }
}

/// <summary>
/// Implementation of DTDC webhook handler.
/// </summary>
public class DTDCWebhookHandler : IDTDCWebhookHandler
{
    private readonly ILogger<DTDCWebhookHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DTDCWebhookHandler(ILogger<DTDCWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task<DTDCWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<DTDCWebhookPayload>(payload, JsonOptions);

            if (webhookPayload == null)
            {
                _logger.LogWarning("Failed to parse DTDC webhook payload");
                return Task.FromResult(new DTDCWebhookResult
                {
                    Success = false,
                    Message = "Invalid payload"
                });
            }

            _logger.LogInformation(
                "DTDC webhook received: Consignment={Consignment}, Status={Status}, StatusCode={StatusCode}",
                webhookPayload.ConsignmentNumber,
                webhookPayload.Status,
                webhookPayload.StatusCode);

            var newStatus = MapDTDCStatus(webhookPayload.StatusCode);

            return Task.FromResult(new DTDCWebhookResult
            {
                Success = true,
                AwbNumber = webhookPayload.ConsignmentNumber,
                OrderId = webhookPayload.ReferenceNumber,
                NewStatus = newStatus,
                Message = webhookPayload.Status,
                Payload = webhookPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DTDC webhook");
            return Task.FromResult(new DTDCWebhookResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Maps DTDC status code to internal ShipmentStatus.
    /// </summary>
    private static ShipmentStatus? MapDTDCStatus(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return null;

        // DTDC Status Codes:
        // BKD - Booked
        // PKD - Picked Up
        // ITR - In Transit
        // ARR - Arrived at Destination
        // OFD - Out for Delivery
        // DLV - Delivered
        // UND - Undelivered
        // RTN - Returned
        // RTO - RTO Initiated
        // CNL - Cancelled
        // LST - Lost
        // DLY - Delayed

        return statusCode.ToUpper() switch
        {
            "BKD" => ShipmentStatus.Manifested,
            "PKD" => ShipmentStatus.PickedUp,
            "ITR" or "ARR" => ShipmentStatus.InTransit,
            "OFD" => ShipmentStatus.OutForDelivery,
            "DLV" => ShipmentStatus.Delivered,
            "UND" or "DLY" => ShipmentStatus.DeliveryFailed,
            "CNL" => ShipmentStatus.Cancelled,
            "RTO" => ShipmentStatus.RTOInitiated,
            "RTN" => ShipmentStatus.RTODelivered,
            "LST" => ShipmentStatus.Lost,
            _ => null
        };
    }
}
