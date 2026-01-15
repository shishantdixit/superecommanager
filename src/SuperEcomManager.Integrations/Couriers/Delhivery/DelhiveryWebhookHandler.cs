using System.Text.Json;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.Delhivery.Models;

namespace SuperEcomManager.Integrations.Couriers.Delhivery;

/// <summary>
/// Handler for Delhivery webhooks.
/// </summary>
public interface IDelhiveryWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload from Delhivery.
    /// </summary>
    Task<DelhiveryWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Delhivery webhook processing.
/// </summary>
public class DelhiveryWebhookResult
{
    public bool Success { get; set; }
    public string? AwbNumber { get; set; }
    public string? OrderId { get; set; }
    public ShipmentStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public DelhiveryWebhookPayload? Payload { get; set; }
}

/// <summary>
/// Implementation of Delhivery webhook handler.
/// </summary>
public class DelhiveryWebhookHandler : IDelhiveryWebhookHandler
{
    private readonly ILogger<DelhiveryWebhookHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DelhiveryWebhookHandler(ILogger<DelhiveryWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task<DelhiveryWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<DelhiveryWebhookPayload>(payload, JsonOptions);

            if (webhookPayload == null)
            {
                _logger.LogWarning("Failed to parse Delhivery webhook payload");
                return Task.FromResult(new DelhiveryWebhookResult
                {
                    Success = false,
                    Message = "Invalid payload"
                });
            }

            _logger.LogInformation(
                "Delhivery webhook received: AWB={Awb}, Status={Status}, StatusCode={StatusCode}",
                webhookPayload.Waybill,
                webhookPayload.Status,
                webhookPayload.StatusCode);

            var newStatus = MapDelhiveryStatus(webhookPayload.StatusCode);

            return Task.FromResult(new DelhiveryWebhookResult
            {
                Success = true,
                AwbNumber = webhookPayload.Waybill,
                OrderId = webhookPayload.ReferenceNumber,
                NewStatus = newStatus,
                Message = webhookPayload.Status,
                Payload = webhookPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Delhivery webhook");
            return Task.FromResult(new DelhiveryWebhookResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Maps Delhivery status code to internal ShipmentStatus.
    /// </summary>
    private static ShipmentStatus? MapDelhiveryStatus(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return null;

        // Delhivery Status Codes:
        // UD - Dispatched (Out for Pickup)
        // PP - Pending Pickup
        // OP - Order Placed / Manifested
        // IT - In Transit
        // RAD - Reached at Destination
        // OC - Out for Delivery
        // DL - Delivered
        // CN - Cancelled
        // CR - Cancellation Requested
        // RTO - RTO Initiated
        // RT - RTO In Transit
        // RTD - RTO Delivered
        // DNA - Delivery Not Attempted
        // NS - Not Serviceable
        // FM - First Mile
        // LM - Last Mile
        // PU - Picked Up
        // ND - Not Delivered (NDR)
        // LT - Lost

        return statusCode.ToUpper() switch
        {
            "UD" or "PP" or "OP" or "FM" => ShipmentStatus.Manifested,
            "PU" => ShipmentStatus.PickedUp,
            "IT" or "RAD" or "LM" => ShipmentStatus.InTransit,
            "OC" => ShipmentStatus.OutForDelivery,
            "DL" => ShipmentStatus.Delivered,
            "CN" or "CR" => ShipmentStatus.Cancelled,
            "RTO" or "RT" => ShipmentStatus.RTOInitiated,
            "RTD" => ShipmentStatus.RTODelivered,
            "ND" or "DNA" => ShipmentStatus.DeliveryFailed,
            "LT" => ShipmentStatus.Lost,
            _ => null
        };
    }
}
