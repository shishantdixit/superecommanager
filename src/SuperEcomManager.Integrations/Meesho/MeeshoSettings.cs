namespace SuperEcomManager.Integrations.Meesho;

/// <summary>
/// Configuration settings for Meesho Supplier API integration.
/// </summary>
public class MeeshoSettings
{
    public const string SectionName = "Meesho";

    /// <summary>
    /// Meesho API Base URL.
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://supplier.meesho.com/api";

    /// <summary>
    /// Default timeout for API calls in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use sandbox mode for testing.
    /// </summary>
    public bool UseSandbox { get; set; } = false;

    /// <summary>
    /// Sandbox API endpoint.
    /// </summary>
    public string SandboxEndpoint { get; set; } = "https://sandbox.supplier.meesho.com/api";
}
