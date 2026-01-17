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

    private HttpRequestMessage CreateRequest(HttpMethod method, string shopDomain, string accessToken, string endpoint)
    {
        var url = $"https://{shopDomain}/admin/api/{_settings.ApiVersion}/{endpoint}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Shopify-Access-Token", accessToken);
        return request;
    }
}
