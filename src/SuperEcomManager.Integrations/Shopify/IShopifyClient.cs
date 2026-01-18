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
    /// Exchanges authorization code for access token using tenant-specific credentials.
    /// </summary>
    Task<ShopifyAccessTokenResponse?> ExchangeCodeForTokenAsync(
        string shopDomain,
        string code,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

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
    /// Creates an order on Shopify.
    /// </summary>
    Task<ShopifyOrder?> CreateOrderAsync(string shopDomain, string accessToken, object orderData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order on Shopify.
    /// </summary>
    Task<ShopifyOrder?> UpdateOrderAsync(string shopDomain, string accessToken, long orderId, object orderData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates OAuth authorization URL using tenant-specific credentials.
    /// </summary>
    string GetAuthorizationUrl(string shopDomain, string redirectUri, string state, string apiKey, string scopes);

    /// <summary>
    /// Verifies webhook HMAC signature.
    /// </summary>
    /// <param name="requestBody">The request body to verify</param>
    /// <param name="hmacHeader">The HMAC header from Shopify</param>
    /// <param name="webhookSecret">Optional tenant-specific webhook secret (falls back to global config)</param>
    bool VerifyWebhookSignature(string requestBody, string hmacHeader, string? webhookSecret = null);

    #region Products

    /// <summary>
    /// Gets products with optional filtering and pagination.
    /// </summary>
    Task<List<ShopifyProduct>> GetProductsAsync(
        string shopDomain,
        string accessToken,
        int limit = 50,
        string? pageInfo = null,
        string? productType = null,
        string? vendor = null,
        DateTime? updatedAtMin = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single product by ID.
    /// </summary>
    Task<ShopifyProduct?> GetProductAsync(string shopDomain, string accessToken, long productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product count.
    /// </summary>
    Task<int> GetProductCountAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory

    /// <summary>
    /// Gets all locations for the shop.
    /// </summary>
    Task<List<ShopifyLocation>> GetLocationsAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory levels for specified inventory item IDs.
    /// </summary>
    Task<List<ShopifyInventoryLevel>> GetInventoryLevelsAsync(
        string shopDomain,
        string accessToken,
        long[] inventoryItemIds,
        long[]? locationIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory levels for a specific location.
    /// </summary>
    Task<List<ShopifyInventoryLevel>> GetInventoryLevelsByLocationAsync(
        string shopDomain,
        string accessToken,
        long locationId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the inventory level for an inventory item at a location.
    /// </summary>
    Task<ShopifyInventoryLevel?> SetInventoryLevelAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        long locationId,
        int available,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts the inventory level for an inventory item at a location.
    /// </summary>
    Task<ShopifyInventoryLevel?> AdjustInventoryLevelAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        long locationId,
        int adjustment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an inventory item by ID.
    /// </summary>
    Task<ShopifyInventoryItem?> GetInventoryItemAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        CancellationToken cancellationToken = default);

    #endregion
}
