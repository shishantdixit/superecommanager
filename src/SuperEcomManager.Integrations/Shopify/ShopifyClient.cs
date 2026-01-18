using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify;

/// <summary>
/// Shopify REST Admin API client implementation.
/// </summary>
public class ShopifyClient : IShopifyClient
{
    private readonly HttpClient _httpClient;
    private readonly ShopifySettings _settings;
    private readonly ILogger<ShopifyClient> _logger;

    public ShopifyClient(
        HttpClient httpClient,
        IOptions<ShopifySettings> settings,
        ILogger<ShopifyClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ShopifyShop?> GetShopAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, "shop.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get shop info: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyShopResponse>(cancellationToken: cancellationToken);
            return result?.Shop;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shop info for {ShopDomain}", shopDomain);
            return null;
        }
    }

    public async Task<List<ShopifyOrder>> GetOrdersAsync(
        string shopDomain,
        string accessToken,
        DateTime? createdAtMin = null,
        DateTime? createdAtMax = null,
        string? status = null,
        int limit = 50,
        string? pageInfo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string> { $"limit={Math.Min(limit, 250)}" };

            if (createdAtMin.HasValue)
                queryParams.Add($"created_at_min={createdAtMin.Value:O}");
            if (createdAtMax.HasValue)
                queryParams.Add($"created_at_max={createdAtMax.Value:O}");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={status}");
            if (!string.IsNullOrEmpty(pageInfo))
                queryParams.Add($"page_info={pageInfo}");

            var endpoint = $"orders.json?{string.Join("&", queryParams)}";
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get orders: {StatusCode}", response.StatusCode);
                return new List<ShopifyOrder>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyOrdersResponse>(cancellationToken: cancellationToken);
            return result?.Orders ?? new List<ShopifyOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for {ShopDomain}", shopDomain);
            return new List<ShopifyOrder>();
        }
    }

    public async Task<ShopifyOrder?> GetOrderAsync(string shopDomain, string accessToken, long orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, $"orders/{orderId}.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get order {OrderId}: {StatusCode}", orderId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyOrderResponse>(cancellationToken: cancellationToken);
            return result?.Order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId} for {ShopDomain}", orderId, shopDomain);
            return null;
        }
    }

    /// <summary>
    /// Exchange OAuth code for access token using tenant-specific credentials.
    /// Note: This method requires the tenant's API credentials.
    /// </summary>
    public async Task<ShopifyAccessTokenResponse?> ExchangeCodeForTokenAsync(
        string shopDomain,
        string code,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://{shopDomain}/admin/oauth/access_token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = apiKey,
                ["client_secret"] = apiSecret,
                ["code"] = code
            });

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to exchange code for token: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShopifyAccessTokenResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for token for {ShopDomain}", shopDomain);
            return null;
        }
    }

    public async Task<ShopifyWebhook?> RegisterWebhookAsync(string shopDomain, string accessToken, string topic, string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, shopDomain, accessToken, "webhooks.json");
            var payload = new
            {
                webhook = new
                {
                    topic,
                    address,
                    format = "json"
                }
            };
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to register webhook {Topic}: {StatusCode} - {Error}", topic, response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyWebhookResponse>(cancellationToken: cancellationToken);
            return result?.Webhook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook {Topic} for {ShopDomain}", topic, shopDomain);
            return null;
        }
    }

    public async Task<List<ShopifyWebhook>> GetWebhooksAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, "webhooks.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get webhooks: {StatusCode}", response.StatusCode);
                return new List<ShopifyWebhook>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyWebhooksResponse>(cancellationToken: cancellationToken);
            return result?.Webhooks ?? new List<ShopifyWebhook>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhooks for {ShopDomain}", shopDomain);
            return new List<ShopifyWebhook>();
        }
    }

    public async Task<bool> DeleteWebhookAsync(string shopDomain, string accessToken, long webhookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Delete, shopDomain, accessToken, $"webhooks/{webhookId}.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {WebhookId} for {ShopDomain}", webhookId, shopDomain);
            return false;
        }
    }

    /// <summary>
    /// Generate OAuth authorization URL using tenant-specific credentials.
    /// </summary>
    public string GetAuthorizationUrl(string shopDomain, string redirectUri, string state, string apiKey, string scopes)
    {
        var encodedRedirectUri = HttpUtility.UrlEncode(redirectUri);

        return $"https://{shopDomain}/admin/oauth/authorize?" +
               $"client_id={apiKey}&" +
               $"scope={scopes}&" +
               $"redirect_uri={encodedRedirectUri}&" +
               $"state={state}";
    }

    /// <summary>
    /// Verify webhook signature using the provided secret.
    /// Falls back to default webhook secret if not provided.
    /// </summary>
    public bool VerifyWebhookSignature(string requestBody, string hmacHeader, string? webhookSecret = null)
    {
        var secret = webhookSecret ?? _settings.WebhookSecret;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(hmacHeader))
            return false;

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedHmac = Convert.ToBase64String(hash);
            return hmacHeader == computedHmac;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ShopifyOrder?> CreateOrderAsync(string shopDomain, string accessToken, object orderData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, shopDomain, accessToken, "orders.json");
            var jsonContent = JsonSerializer.Serialize(orderData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to create order on Shopify: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyOrderResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Created order on Shopify: {OrderId}", result?.Order?.Id);
            return result?.Order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order on Shopify for {ShopDomain}", shopDomain);
            return null;
        }
    }

    public async Task<ShopifyOrder?> UpdateOrderAsync(string shopDomain, string accessToken, long orderId, object orderData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Put, shopDomain, accessToken, $"orders/{orderId}.json");
            var jsonContent = JsonSerializer.Serialize(orderData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to update order on Shopify: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyOrderResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Updated order on Shopify: {OrderId}", result?.Order?.Id);
            return result?.Order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} on Shopify for {ShopDomain}", orderId, shopDomain);
            return null;
        }
    }

    #region Products

    public async Task<List<ShopifyProduct>> GetProductsAsync(
        string shopDomain,
        string accessToken,
        int limit = 50,
        string? pageInfo = null,
        string? productType = null,
        string? vendor = null,
        DateTime? updatedAtMin = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string> { $"limit={Math.Min(limit, 250)}" };

            if (!string.IsNullOrEmpty(pageInfo))
                queryParams.Add($"page_info={pageInfo}");
            if (!string.IsNullOrEmpty(productType))
                queryParams.Add($"product_type={HttpUtility.UrlEncode(productType)}");
            if (!string.IsNullOrEmpty(vendor))
                queryParams.Add($"vendor={HttpUtility.UrlEncode(vendor)}");
            if (updatedAtMin.HasValue)
                queryParams.Add($"updated_at_min={updatedAtMin.Value:O}");

            var endpoint = $"products.json?{string.Join("&", queryParams)}";
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get products: {StatusCode}", response.StatusCode);
                return new List<ShopifyProduct>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyProductsResponse>(cancellationToken: cancellationToken);
            return result?.Products ?? new List<ShopifyProduct>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for {ShopDomain}", shopDomain);
            return new List<ShopifyProduct>();
        }
    }

    public async Task<ShopifyProduct?> GetProductAsync(string shopDomain, string accessToken, long productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, $"products/{productId}.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get product {ProductId}: {StatusCode}", productId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyProductResponse>(cancellationToken: cancellationToken);
            return result?.Product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} for {ShopDomain}", productId, shopDomain);
            return null;
        }
    }

    public async Task<int> GetProductCountAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, "products/count.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get product count: {StatusCode}", response.StatusCode);
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyCountResponse>(cancellationToken: cancellationToken);
            return result?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product count for {ShopDomain}", shopDomain);
            return 0;
        }
    }

    #endregion

    #region Inventory

    public async Task<List<ShopifyLocation>> GetLocationsAsync(string shopDomain, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, "locations.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get locations: {StatusCode}", response.StatusCode);
                return new List<ShopifyLocation>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyLocationsResponse>(cancellationToken: cancellationToken);
            return result?.Locations ?? new List<ShopifyLocation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for {ShopDomain}", shopDomain);
            return new List<ShopifyLocation>();
        }
    }

    public async Task<List<ShopifyInventoryLevel>> GetInventoryLevelsAsync(
        string shopDomain,
        string accessToken,
        long[] inventoryItemIds,
        long[]? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"inventory_item_ids={string.Join(",", inventoryItemIds)}"
            };

            if (locationIds != null && locationIds.Length > 0)
                queryParams.Add($"location_ids={string.Join(",", locationIds)}");

            var endpoint = $"inventory_levels.json?{string.Join("&", queryParams)}";
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get inventory levels: {StatusCode}", response.StatusCode);
                return new List<ShopifyInventoryLevel>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyInventoryLevelsResponse>(cancellationToken: cancellationToken);
            return result?.InventoryLevels ?? new List<ShopifyInventoryLevel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory levels for {ShopDomain}", shopDomain);
            return new List<ShopifyInventoryLevel>();
        }
    }

    public async Task<List<ShopifyInventoryLevel>> GetInventoryLevelsByLocationAsync(
        string shopDomain,
        string accessToken,
        long locationId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"inventory_levels.json?location_ids={locationId}&limit={Math.Min(limit, 250)}";
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get inventory levels for location {LocationId}: {StatusCode}", locationId, response.StatusCode);
                return new List<ShopifyInventoryLevel>();
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyInventoryLevelsResponse>(cancellationToken: cancellationToken);
            return result?.InventoryLevels ?? new List<ShopifyInventoryLevel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory levels for location {LocationId} on {ShopDomain}", locationId, shopDomain);
            return new List<ShopifyInventoryLevel>();
        }
    }

    public async Task<ShopifyInventoryLevel?> SetInventoryLevelAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        long locationId,
        int available,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, shopDomain, accessToken, "inventory_levels/set.json");
            var payload = new ShopifySetInventoryLevelRequest
            {
                LocationId = locationId,
                InventoryItemId = inventoryItemId,
                Available = available
            };
            request.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to set inventory level: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyInventoryLevelResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Set inventory level for item {InventoryItemId} at location {LocationId} to {Available}",
                inventoryItemId, locationId, available);
            return result?.InventoryLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting inventory level for item {InventoryItemId} on {ShopDomain}", inventoryItemId, shopDomain);
            return null;
        }
    }

    public async Task<ShopifyInventoryLevel?> AdjustInventoryLevelAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        long locationId,
        int adjustment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, shopDomain, accessToken, "inventory_levels/adjust.json");
            var payload = new ShopifyAdjustInventoryLevelRequest
            {
                LocationId = locationId,
                InventoryItemId = inventoryItemId,
                AvailableAdjustment = adjustment
            };
            request.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to adjust inventory level: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyInventoryLevelResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Adjusted inventory level for item {InventoryItemId} at location {LocationId} by {Adjustment}",
                inventoryItemId, locationId, adjustment);
            return result?.InventoryLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting inventory level for item {InventoryItemId} on {ShopDomain}", inventoryItemId, shopDomain);
            return null;
        }
    }

    public async Task<ShopifyInventoryItem?> GetInventoryItemAsync(
        string shopDomain,
        string accessToken,
        long inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, shopDomain, accessToken, $"inventory_items/{inventoryItemId}.json");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get inventory item {InventoryItemId}: {StatusCode}", inventoryItemId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShopifyInventoryItemResponse>(cancellationToken: cancellationToken);
            return result?.InventoryItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item {InventoryItemId} for {ShopDomain}", inventoryItemId, shopDomain);
            return null;
        }
    }

    #endregion

    private HttpRequestMessage CreateRequest(HttpMethod method, string shopDomain, string accessToken, string endpoint)
    {
        var url = $"https://{shopDomain}/admin/api/{_settings.ApiVersion}/{endpoint}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Shopify-Access-Token", accessToken);
        return request;
    }
}
