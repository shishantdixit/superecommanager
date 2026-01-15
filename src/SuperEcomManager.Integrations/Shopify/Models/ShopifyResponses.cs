using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Shopify.Models;

/// <summary>
/// Wrapper for Shopify orders list response.
/// </summary>
public class ShopifyOrdersResponse
{
    [JsonPropertyName("orders")]
    public List<ShopifyOrder> Orders { get; set; } = new();
}

/// <summary>
/// Wrapper for single Shopify order response.
/// </summary>
public class ShopifyOrderResponse
{
    [JsonPropertyName("order")]
    public ShopifyOrder? Order { get; set; }
}

/// <summary>
/// Shopify shop information.
/// </summary>
public class ShopifyShop
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("myshopify_domain")]
    public string MyshopifyDomain { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("iana_timezone")]
    public string IanaTimezone { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("plan_name")]
    public string? PlanName { get; set; }
}

public class ShopifyShopResponse
{
    [JsonPropertyName("shop")]
    public ShopifyShop? Shop { get; set; }
}

/// <summary>
/// Shopify OAuth access token response.
/// </summary>
public class ShopifyAccessTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Shopify webhook registration.
/// </summary>
public class ShopifyWebhook
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ShopifyWebhookResponse
{
    [JsonPropertyName("webhook")]
    public ShopifyWebhook? Webhook { get; set; }
}

public class ShopifyWebhooksResponse
{
    [JsonPropertyName("webhooks")]
    public List<ShopifyWebhook> Webhooks { get; set; } = new();
}

/// <summary>
/// Shopify API error response.
/// </summary>
public class ShopifyErrorResponse
{
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}
