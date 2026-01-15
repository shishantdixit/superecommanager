namespace SuperEcomManager.Integrations.Couriers.Delhivery;

/// <summary>
/// Configuration settings for Delhivery API integration.
/// </summary>
public class DelhiverySettings
{
    public const string SectionName = "Delhivery";

    /// <summary>
    /// Base URL for Delhivery API.
    /// Production: https://track.delhivery.com
    /// Staging: https://staging-express.delhivery.com
    /// </summary>
    public string BaseUrl { get; set; } = "https://track.delhivery.com";

    /// <summary>
    /// Base URL for Delhivery tracking API.
    /// </summary>
    public string TrackingUrl { get; set; } = "https://track.delhivery.com";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
