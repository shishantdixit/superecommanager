namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// Configuration settings for Shiprocket API.
/// </summary>
public class ShiprocketSettings
{
    public const string SectionName = "Shiprocket";

    /// <summary>
    /// Base URL for Shiprocket API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://apiv2.shiprocket.in/v1/external";

    /// <summary>
    /// API timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Webhook secret for signature verification.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Default pickup location ID (can be overridden per account).
    /// </summary>
    public int? DefaultPickupLocationId { get; set; }

    /// <summary>
    /// Default channel ID for orders.
    /// </summary>
    public int? DefaultChannelId { get; set; }
}
