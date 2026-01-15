namespace SuperEcomManager.Integrations.Shopify;

/// <summary>
/// Shopify API configuration settings.
/// </summary>
public class ShopifySettings
{
    public const string SectionName = "Shopify";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string WebhookSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-01";
}
