namespace SuperEcomManager.Integrations.Couriers.DTDC;

/// <summary>
/// Configuration settings for DTDC API integration.
/// </summary>
public class DTDCSettings
{
    public const string SectionName = "DTDC";

    /// <summary>
    /// Base URL for DTDC API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.dtdc.com";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
