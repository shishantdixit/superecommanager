using System.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to initiate Shopify OAuth flow.
/// Uses tenant-specific credentials stored in the SalesChannel entity.
/// </summary>
[RequirePermission("channels.connect")]
[RequireFeature("channels_management")]
public record InitiateShopifyOAuthCommand : IRequest<Result<ShopifyOAuthResult>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
    public string RedirectUri { get; init; } = string.Empty;
}

public class ShopifyOAuthResult
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class InitiateShopifyOAuthCommandHandler : IRequestHandler<InitiateShopifyOAuthCommand, Result<ShopifyOAuthResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<InitiateShopifyOAuthCommandHandler> _logger;

    public InitiateShopifyOAuthCommandHandler(
        ITenantDbContext dbContext,
        ICacheService cacheService,
        ICurrentTenantService currentTenantService,
        ILogger<InitiateShopifyOAuthCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<Result<ShopifyOAuthResult>> Handle(
        InitiateShopifyOAuthCommand request,
        CancellationToken cancellationToken)
    {
        // Get the channel with credentials
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<ShopifyOAuthResult>.Failure("Channel not found");
        }

        if (channel.Type != ChannelType.Shopify)
        {
            return Result<ShopifyOAuthResult>.Failure("This channel is not a Shopify channel");
        }

        // Check if credentials are configured
        if (string.IsNullOrEmpty(channel.ApiKey) || string.IsNullOrEmpty(channel.ApiSecret))
        {
            return Result<ShopifyOAuthResult>.Failure("Shopify API credentials not configured. Please save your API Key and Secret first.");
        }

        if (string.IsNullOrEmpty(channel.StoreUrl))
        {
            return Result<ShopifyOAuthResult>.Failure("Shop domain not configured");
        }

        // Extract shop domain from store URL
        var shopDomain = channel.StoreUrl.Replace("https://", "").Replace("http://", "").TrimEnd('/');

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString("N");

        // Store state in GLOBAL cache for verification during callback (10 minutes expiry)
        // Use global cache because callback comes without tenant context
        // Include channel ID, shop domain, tenant ID, and schema name for context restoration
        var stateData = $"{channel.Id}|{shopDomain}|{_currentTenantService.TenantId}|{_currentTenantService.SchemaName}";
        var stateKey = $"shopify_oauth_state:{state}";
        await _cacheService.SetGlobalAsync(stateKey, stateData, TimeSpan.FromMinutes(10), cancellationToken);

        // Build Shopify OAuth URL using tenant's credentials
        var scopes = channel.Scopes ?? "read_orders,write_orders,read_products,read_inventory,write_inventory,read_fulfillments,write_fulfillments";
        var authUrl = BuildAuthorizationUrl(shopDomain, channel.ApiKey, scopes, request.RedirectUri, state);

        _logger.LogInformation(
            "Initiated Shopify OAuth for channel {ChannelId}, shop domain: {ShopDomain}",
            channel.Id, shopDomain);

        return Result<ShopifyOAuthResult>.Success(new ShopifyOAuthResult
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }

    private static string BuildAuthorizationUrl(string shopDomain, string apiKey, string scopes, string redirectUri, string state)
    {
        var baseUrl = $"https://{shopDomain}/admin/oauth/authorize";
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["client_id"] = apiKey;
        queryParams["scope"] = scopes;
        queryParams["redirect_uri"] = redirectUri;
        queryParams["state"] = state;

        return $"{baseUrl}?{queryParams}";
    }
}
