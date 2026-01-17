using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to complete Shopify OAuth flow after callback.
/// This command does NOT require tenant context - it extracts tenant info from the OAuth state.
/// Uses tenant-specific credentials stored in the SalesChannel entity.
/// </summary>
public record CompleteShopifyOAuthCommand : IRequest<Result<ChannelDto>>
{
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ShopDomain { get; init; } = string.Empty;
}

public class CompleteShopifyOAuthCommandHandler : IRequestHandler<CompleteShopifyOAuthCommand, Result<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CompleteShopifyOAuthCommandHandler> _logger;

    public CompleteShopifyOAuthCommandHandler(
        ITenantDbContext dbContext,
        ICacheService cacheService,
        ICurrentTenantService currentTenantService,
        IHttpClientFactory httpClientFactory,
        ILogger<CompleteShopifyOAuthCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentTenantService = currentTenantService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<ChannelDto>> Handle(CompleteShopifyOAuthCommand request, CancellationToken cancellationToken)
    {
        // Verify state matches (OAuth CSRF protection)
        // Use GLOBAL cache because this callback comes without tenant context
        var stateKey = $"shopify_oauth_state:{request.State}";
        var storedStateData = await _cacheService.GetGlobalAsync<string>(stateKey, cancellationToken);

        if (string.IsNullOrEmpty(storedStateData))
        {
            _logger.LogWarning("Invalid or expired OAuth state: {State}", request.State);
            return Result<ChannelDto>.Failure("Invalid or expired OAuth state");
        }

        // Remove used state from global cache
        await _cacheService.RemoveGlobalAsync(stateKey, cancellationToken);

        // Parse stored state data (format: channelId|shopDomain|tenantId|schemaName)
        var stateParts = storedStateData.Split('|');
        if (stateParts.Length < 4 ||
            !Guid.TryParse(stateParts[0], out var channelId) ||
            !Guid.TryParse(stateParts[2], out var tenantId))
        {
            _logger.LogWarning("Invalid state data format: {StateData}", storedStateData);
            return Result<ChannelDto>.Failure("Invalid OAuth state data");
        }

        var expectedShopDomain = stateParts[1];
        var schemaName = stateParts[3];

        // Set tenant context for database access
        _currentTenantService.SetTenant(tenantId, schemaName, schemaName);

        // Verify shop domain matches
        var requestShopDomain = request.ShopDomain.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        if (!string.Equals(expectedShopDomain, requestShopDomain, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Shop domain mismatch. Expected: {Expected}, Got: {Actual}",
                expectedShopDomain, requestShopDomain);
            return Result<ChannelDto>.Failure("Shop domain mismatch");
        }

        // Get the channel with credentials
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return Result<ChannelDto>.Failure("Channel not found");
        }

        if (string.IsNullOrEmpty(channel.ApiKey) || string.IsNullOrEmpty(channel.ApiSecret))
        {
            return Result<ChannelDto>.Failure("Channel credentials not configured");
        }

        // Exchange code for access token using tenant's credentials
        var (accessToken, storeName, error) = await ExchangeCodeForToken(
            expectedShopDomain,
            request.Code,
            channel.ApiKey,
            channel.ApiSecret,
            cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning(
                "Failed to exchange OAuth code for channel {ChannelId}, shop: {ShopDomain}, error: {Error}",
                channelId, expectedShopDomain, error);
            channel.MarkDisconnected(error ?? "Failed to connect");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<ChannelDto>.Failure(error ?? "Failed to connect to Shopify. Please try again.");
        }

        // Update channel with access token and mark as connected
        channel.SetAccessToken(accessToken);
        channel.MarkConnected();
        if (!string.IsNullOrEmpty(storeName))
        {
            channel.UpdateStoreName(storeName);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully connected Shopify store for channel {ChannelId}, shop: {ShopDomain}",
            channelId, expectedShopDomain);

        return Result<ChannelDto>.Success(new ChannelDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Type = channel.Type,
            IsActive = channel.IsActive,
            StoreUrl = channel.StoreUrl,
            StoreName = channel.StoreName,
            LastSyncAt = channel.LastSyncAt,
            TotalOrders = 0,
            SyncStatus = ChannelSyncStatus.NotStarted,
            CreatedAt = channel.CreatedAt,
            AutoSyncOrders = channel.AutoSyncOrders,
            AutoSyncInventory = channel.AutoSyncInventory,
            IsConnected = channel.IsConnected,
            HasCredentials = true
        });
    }

    private async Task<(string? AccessToken, string? StoreName, string? Error)> ExchangeCodeForToken(
        string shopDomain,
        string code,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenUrl = $"https://{shopDomain}/admin/oauth/access_token";

            // Shopify requires form-urlencoded content for token exchange
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = apiKey,
                ["client_secret"] = apiSecret,
                ["code"] = code
            });

            var response = await client.PostAsync(tokenUrl, formContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Shopify token exchange failed with status {Status}: {Error}",
                    response.StatusCode, errorContent);
                return (null, null, $"Shopify returned error: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<ShopifyTokenResponse>(cancellationToken: cancellationToken);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Invalid token response from Shopify: {Response}", responseContent);
                return (null, null, "Invalid response from Shopify");
            }

            // Optionally fetch shop info to get the store name
            string? storeName = null;
            try
            {
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", tokenResponse.access_token);
                var shopResponse = await client.GetAsync($"https://{shopDomain}/admin/api/2024-01/shop.json", cancellationToken);
                if (shopResponse.IsSuccessStatusCode)
                {
                    var shopData = await shopResponse.Content.ReadFromJsonAsync<ShopifyShopResponse>(cancellationToken: cancellationToken);
                    storeName = shopData?.shop?.name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch shop info for {ShopDomain}", shopDomain);
            }

            return (tokenResponse.access_token, storeName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging OAuth code for {ShopDomain}", shopDomain);
            return (null, null, "Failed to connect to Shopify");
        }
    }

    // Use snake_case to match Shopify API response
    private class ShopifyTokenResponse
    {
        public string? access_token { get; set; }
        public string? scope { get; set; }
    }

    private class ShopifyShopResponse
    {
        public ShopifyShop? shop { get; set; }
    }

    private class ShopifyShop
    {
        public string? name { get; set; }
    }
}
