namespace SuperEcomManager.Integrations.Shopify;

/// <summary>
/// Shopify API configuration settings.
/// Note: API credentials (ApiKey, ApiSecret) are now stored per-tenant in SalesChannel.
/// This settings class contains only shared configuration like API version.
/// </summary>
public class ShopifySettings
{
    public const string SectionName = "Shopify";

    /// <summary>
    /// Default Shopify API version to use.
    /// </summary>
    public string ApiVersion { get; set; } = "2024-01";

    /// <summary>
    /// Default webhook secret for verifying Shopify webhooks (per-tenant secrets override this).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
