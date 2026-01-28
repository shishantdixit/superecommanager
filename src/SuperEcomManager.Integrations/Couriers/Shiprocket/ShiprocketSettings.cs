namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// Configuration settings for Shiprocket API.
///
/// SETUP INSTRUCTIONS:
/// Shiprocket requires a dedicated API user for programmatic access.
/// Regular user accounts use OTP-based authentication which is NOT suitable for automated API calls.
///
/// To set up Shiprocket API access:
/// 1. Log in to Shiprocket dashboard (https://app.shiprocket.in)
/// 2. Go to Settings → API → API Users
/// 3. Create a new API user with email and password
/// 4. Use these API user credentials (NOT your regular login) when configuring
///    the courier account in the platform
///
/// The API user credentials are stored in the CourierAccount entity as ApiKey (email) and ApiSecret (password).
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
