using MediatR;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to initiate Shopify OAuth flow.
/// </summary>
[RequirePermission("channels.connect")]
[RequireFeature("channels_management")]
public record InitiateShopifyOAuthCommand : IRequest<Result<ShopifyOAuthResult>>, ITenantRequest
{
    public string ShopDomain { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}

public class ShopifyOAuthResult
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class InitiateShopifyOAuthCommandHandler : IRequestHandler<InitiateShopifyOAuthCommand, Result<ShopifyOAuthResult>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<InitiateShopifyOAuthCommandHandler> _logger;

    // Delegate for generating authorization URL (injected from Integrations layer)
    public Func<string, string, string, string>? GetAuthorizationUrl { get; set; }

    public InitiateShopifyOAuthCommandHandler(
        ICacheService cacheService,
        ILogger<InitiateShopifyOAuthCommandHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<ShopifyOAuthResult>> Handle(
        InitiateShopifyOAuthCommand request,
        CancellationToken cancellationToken)
    {
        // Validate shop domain format
        if (string.IsNullOrWhiteSpace(request.ShopDomain) ||
            !request.ShopDomain.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ShopifyOAuthResult>.Failure("Invalid shop domain. Must end with .myshopify.com");
        }

        if (GetAuthorizationUrl == null)
        {
            _logger.LogError("GetAuthorizationUrl delegate not configured");
            return Result<ShopifyOAuthResult>.Failure("OAuth configuration error");
        }

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString("N");

        // Store state in cache for verification during callback (10 minutes expiry)
        var stateKey = $"shopify_oauth_state:{state}";
        await _cacheService.SetAsync(stateKey, request.ShopDomain, TimeSpan.FromMinutes(10), cancellationToken);

        // Generate authorization URL
        var authUrl = GetAuthorizationUrl(request.ShopDomain, request.RedirectUri, state);

        _logger.LogInformation("Initiated Shopify OAuth for {ShopDomain}", request.ShopDomain);

        return Result<ShopifyOAuthResult>.Success(new ShopifyOAuthResult
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }
}
