using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// Handler for Shiprocket webhooks.
/// </summary>
public interface IShiprocketWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload from Shiprocket.
    /// </summary>
    Task<ShiprocketWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of webhook processing.
/// </summary>
public class ShiprocketWebhookResult
{
    public bool Success { get; set; }
    public string? AwbNumber { get; set; }
    public string? OrderId { get; set; }
    public ShipmentStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public ShiprocketWebhookPayload? Payload { get; set; }
}

/// <summary>
/// Shiprocket webhook payload model.
/// </summary>
public class ShiprocketWebhookPayload
{
    [JsonPropertyName("awb")]
    public string? Awb { get; set; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long? ShipmentId { get; set; }

    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("current_status_id")]
    public int CurrentStatusId { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }

    [JsonPropertyName("scans")]
    public List<ShiprocketScan>? Scans { get; set; }

    [JsonPropertyName("etd")]
    public string? Etd { get; set; }

    [JsonPropertyName("current_timestamp")]
    public string? CurrentTimestamp { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("pod")]
    public string? Pod { get; set; }

    [JsonPropertyName("pod_image")]
    public string? PodImage { get; set; }

    [JsonPropertyName("delivered_to")]
    public string? DeliveredTo { get; set; }
}

public class ShiprocketScan
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("activity")]
    public string? Activity { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Implementation of Shiprocket webhook handler.
/// </summary>
public class ShiprocketWebhookHandler : IShiprocketWebhookHandler
{
    private readonly ILogger<ShiprocketWebhookHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ShiprocketWebhookHandler(ILogger<ShiprocketWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task<ShiprocketWebhookResult> HandleWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<ShiprocketWebhookPayload>(payload, JsonOptions);

            if (webhookPayload == null)
            {
                _logger.LogWarning("Failed to parse Shiprocket webhook payload");
                return Task.FromResult(new ShiprocketWebhookResult
                {
                    Success = false,
                    Message = "Invalid payload"
                });
            }

            _logger.LogInformation(
                "Shiprocket webhook received: AWB={Awb}, Status={Status}, StatusId={StatusId}",
                webhookPayload.Awb,
                webhookPayload.CurrentStatus,
                webhookPayload.CurrentStatusId);

            var newStatus = MapShiprocketStatus(webhookPayload.CurrentStatusId);

            return Task.FromResult(new ShiprocketWebhookResult
            {
                Success = true,
                AwbNumber = webhookPayload.Awb,
                OrderId = webhookPayload.OrderId,
                NewStatus = newStatus,
                Message = webhookPayload.CurrentStatus,
                Payload = webhookPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shiprocket webhook");
            return Task.FromResult(new ShiprocketWebhookResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Maps Shiprocket status ID to internal ShipmentStatus.
    /// </summary>
    private static ShipmentStatus? MapShiprocketStatus(int statusId)
    {
        // Shiprocket status IDs:
        // 1 - AWB Assigned
        // 2 - Label Generated
        // 3 - Pickup Scheduled/Generated
        // 4 - Pickup Queued
        // 5 - Manifest Generated
        // 6 - Shipped
        // 7 - Delivered
        // 8 - Cancelled
        // 9 - RTO Initiated
        // 10 - RTO Delivered
        // 11 - Lost
        // 12 - NDR
        // 13 - Out for Delivery
        // 14 - Pickup Exception
        // 15 - Pickup Rescheduled
        // 16 - In Transit
        // 17 - Out for Pickup
        // 18 - Picked Up
        // 19 - RTO Acknowledged
        // 20 - RTO In Transit

        return statusId switch
        {
            1 or 2 or 3 or 4 or 5 => ShipmentStatus.Manifested,
            6 or 16 => ShipmentStatus.InTransit,
            7 => ShipmentStatus.Delivered,
            8 => ShipmentStatus.Cancelled,
            9 or 19 or 20 => ShipmentStatus.RTOInitiated,
            10 => ShipmentStatus.RTODelivered,
            11 => ShipmentStatus.Lost,
            12 => ShipmentStatus.DeliveryFailed,
            13 => ShipmentStatus.OutForDelivery,
            17 or 18 => ShipmentStatus.PickedUp,
            _ => null
        };
    }
}
