using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to complete Shopify OAuth flow after callback.
/// </summary>
[RequirePermission("channels.connect")]
[RequireFeature("channels_management")]
public record CompleteShopifyOAuthCommand : IRequest<Result<ChannelDto>>, ITenantRequest
{
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ShopDomain { get; init; } = string.Empty;
}

public class CompleteShopifyOAuthCommandHandler : IRequestHandler<CompleteShopifyOAuthCommand, Result<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CompleteShopifyOAuthCommandHandler> _logger;

    // This will be injected from Integrations layer
    public Func<string, string, CancellationToken, Task<(string? AccessToken, string? StoreName)>>? ExchangeCodeForToken { get; set; }

    public CompleteShopifyOAuthCommandHandler(
        ITenantDbContext dbContext,
        ICacheService cacheService,
        ILogger<CompleteShopifyOAuthCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<ChannelDto>> Handle(CompleteShopifyOAuthCommand request, CancellationToken cancellationToken)
    {
        // Verify state matches (OAuth CSRF protection)
        var stateKey = $"shopify_oauth_state:{request.State}";
        var storedState = await _cacheService.GetAsync<string>(stateKey, cancellationToken);

        if (storedState == null)
        {
            _logger.LogWarning("Invalid or expired OAuth state: {State}", request.State);
            return Result<ChannelDto>.Failure("Invalid or expired OAuth state");
        }

        // Remove used state
        await _cacheService.RemoveAsync(stateKey, cancellationToken);

        // Check if channel already exists for this shop
        var existingChannel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.StoreUrl == request.ShopDomain && c.Type == ChannelType.Shopify, cancellationToken);

        if (existingChannel != null && existingChannel.IsActive)
        {
            return Result<ChannelDto>.Failure("This Shopify store is already connected");
        }

        // Exchange code for access token (handled by integration layer)
        if (ExchangeCodeForToken == null)
        {
            _logger.LogError("ExchangeCodeForToken delegate not configured");
            return Result<ChannelDto>.Failure("OAuth configuration error");
        }

        var (accessToken, storeName) = await ExchangeCodeForToken(request.ShopDomain, request.Code, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Failed to exchange OAuth code for {ShopDomain}", request.ShopDomain);
            return Result<ChannelDto>.Failure("Failed to connect to Shopify. Please try again.");
        }

        // Create or update channel
        SalesChannel channel;
        if (existingChannel != null)
        {
            existingChannel.Activate();
            existingChannel.UpdateCredentials(accessToken);
            existingChannel.UpdateStoreName(storeName ?? request.ShopDomain);
            channel = existingChannel;
        }
        else
        {
            channel = SalesChannel.Create(
                name: storeName ?? request.ShopDomain,
                type: ChannelType.Shopify,
                storeUrl: request.ShopDomain,
                storeName: storeName
            );
            channel.UpdateCredentials(accessToken);
            await _dbContext.SalesChannels.AddAsync(channel, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully connected Shopify store {ShopDomain}", request.ShopDomain);

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
            CreatedAt = channel.CreatedAt
        });
    }
}
