namespace SuperEcomManager.Integrations.Couriers.BlueDart;

/// <summary>
/// Configuration settings for BlueDart API integration.
/// </summary>
public class BlueDartSettings
{
    public const string SectionName = "BlueDart";

    /// <summary>
    /// Base URL for BlueDart API.
    /// Production: https://netconnect.bluedart.com
    /// Staging: https://netconnectuat.bluedart.com
    /// </summary>
    public string BaseUrl { get; set; } = "https://netconnect.bluedart.com";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
