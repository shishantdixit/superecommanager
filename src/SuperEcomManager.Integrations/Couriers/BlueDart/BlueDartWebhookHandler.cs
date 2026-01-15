using System.Text.Json;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.BlueDart.Models;

namespace SuperEcomManager.Integrations.Couriers.BlueDart;

/// <summary>
/// Handler for BlueDart webhooks/push notifications.
/// </summary>
public interface IBlueDartWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload from BlueDart.
    /// </summary>
    Task<BlueDartWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of BlueDart webhook processing.
/// </summary>
public class BlueDartWebhookResult
{
    public bool Success { get; set; }
    public string? AwbNumber { get; set; }
    public string? OrderId { get; set; }
    public ShipmentStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public BlueDartWebhookPayload? Payload { get; set; }
}

/// <summary>
/// Implementation of BlueDart webhook handler.
/// </summary>
public class BlueDartWebhookHandler : IBlueDartWebhookHandler
{
    private readonly ILogger<BlueDartWebhookHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BlueDartWebhookHandler(ILogger<BlueDartWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task<BlueDartWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<BlueDartWebhookPayload>(payload, JsonOptions);

            if (webhookPayload == null)
            {
                _logger.LogWarning("Failed to parse BlueDart webhook payload");
                return Task.FromResult(new BlueDartWebhookResult
                {
                    Success = false,
                    Message = "Invalid payload"
                });
            }

            _logger.LogInformation(
                "BlueDart webhook received: AWB={Awb}, Status={Status}, StatusCode={StatusCode}",
                webhookPayload.AwbNo,
                webhookPayload.Status,
                webhookPayload.StatusCode);

            var newStatus = MapBlueDartStatus(webhookPayload.StatusCode);

            return Task.FromResult(new BlueDartWebhookResult
            {
                Success = true,
                AwbNumber = webhookPayload.AwbNo,
                OrderId = webhookPayload.ReferenceNo,
                NewStatus = newStatus,
                Message = webhookPayload.Status,
                Payload = webhookPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BlueDart webhook");
            return Task.FromResult(new BlueDartWebhookResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Maps BlueDart status code to internal ShipmentStatus.
    /// </summary>
    private static ShipmentStatus? MapBlueDartStatus(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return null;

        // BlueDart Status Codes:
        // PKD - Picked Up
        // IT - In Transit
        // LD - Arrived at Location
        // OD - Out for Delivery
        // DL - Delivered
        // ND - Not Delivered
        // HD - Holiday
        // CN - Cancelled
        // RTO - Return to Origin
        // RTD - RTO Delivered
        // PKF - Pickup Failed
        // DLE - Delivery Exception
        // LST - Lost

        return statusCode.ToUpper() switch
        {
            "PKD" => ShipmentStatus.PickedUp,
            "IT" or "LD" => ShipmentStatus.InTransit,
            "OD" => ShipmentStatus.OutForDelivery,
            "DL" => ShipmentStatus.Delivered,
            "ND" or "DLE" or "HD" => ShipmentStatus.DeliveryFailed,
            "CN" => ShipmentStatus.Cancelled,
            "RTO" => ShipmentStatus.RTOInitiated,
            "RTD" => ShipmentStatus.RTODelivered,
            "PKF" => ShipmentStatus.Manifested, // Still pending pickup
            "LST" => ShipmentStatus.Lost,
            _ => null
        };
    }
}
