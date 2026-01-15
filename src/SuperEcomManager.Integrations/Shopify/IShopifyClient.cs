using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify;

/// <summary>
/// Interface for Shopify API client.
/// </summary>
public interface IShopifyClient
{
    /// <summary>
    /// Gets shop information.
    /// </summary>
    Task<ShopifyShop?> GetShopAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders with optional filtering.
    /// </summary>
    Task<List<ShopifyOrder>> GetOrdersAsync(
        string shopDomain,
        string accessToken,
        DateTime? createdAtMin = null,
        DateTime? createdAtMax = null,
        string? status = null,
        int limit = 50,
        string? pageInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single order by ID.
    /// </summary>
    Task<ShopifyOrder?> GetOrderAsync(string shopDomain, string accessToken, long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    Task<ShopifyAccessTokenResponse?> ExchangeCodeForTokenAsync(string shopDomain, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a webhook.
    /// </summary>
    Task<ShopifyWebhook?> RegisterWebhookAsync(string shopDomain, string accessToken, string topic, string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered webhooks.
    /// </summary>
    Task<List<ShopifyWebhook>> GetWebhooksAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    Task<bool> DeleteWebhookAsync(string shopDomain, string accessToken, long webhookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates OAuth authorization URL.
    /// </summary>
    string GetAuthorizationUrl(string shopDomain, string redirectUri, string state);

    /// <summary>
    /// Verifies webhook HMAC signature.
    /// </summary>
    bool VerifyWebhookSignature(string requestBody, string hmacHeader);
}
