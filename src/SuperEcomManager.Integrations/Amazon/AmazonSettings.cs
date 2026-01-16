namespace SuperEcomManager.Integrations.Amazon;

/// <summary>
/// Configuration settings for Amazon SP-API integration.
/// </summary>
public class AmazonSettings
{
    public const string SectionName = "Amazon";

    /// <summary>
    /// Amazon SP-API Client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Amazon SP-API Client Secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// AWS Access Key for API signing.
    /// </summary>
    public string AwsAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Secret Key for API signing.
    /// </summary>
    public string AwsSecretKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region for SP-API (default: eu-west-1 for India).
    /// </summary>
    public string AwsRegion { get; set; } = "eu-west-1";

    /// <summary>
    /// SP-API endpoint base URL.
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://sellingpartnerapi-eu.amazon.com";

    /// <summary>
    /// OAuth endpoint for token exchange.
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://api.amazon.com/auth/o2/token";

    /// <summary>
    /// Marketplace ID for Amazon India.
    /// </summary>
    public string MarketplaceId { get; set; } = "A21TJRUUN4KGV";

    /// <summary>
    /// Whether to use sandbox mode for testing.
    /// </summary>
    public bool UseSandbox { get; set; } = false;

    /// <summary>
    /// Sandbox endpoint URL.
    /// </summary>
    public string SandboxEndpoint { get; set; } = "https://sandbox.sellingpartnerapi-eu.amazon.com";
}
