namespace SuperEcomManager.Integrations.Flipkart;

/// <summary>
/// Configuration settings for Flipkart Seller API integration.
/// </summary>
public class FlipkartSettings
{
    public const string SectionName = "Flipkart";

    /// <summary>
    /// Flipkart API Base URL.
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://api.flipkart.net/sellers";

    /// <summary>
    /// Flipkart OAuth Token Endpoint.
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://api.flipkart.net/oauth-service/oauth/token";

    /// <summary>
    /// Application ID for API access.
    /// </summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Application Secret for API access.
    /// </summary>
    public string ApplicationSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use sandbox mode for testing.
    /// </summary>
    public bool UseSandbox { get; set; } = false;

    /// <summary>
    /// Sandbox API endpoint.
    /// </summary>
    public string SandboxEndpoint { get; set; } = "https://sandbox-api.flipkart.net/sellers";
}
