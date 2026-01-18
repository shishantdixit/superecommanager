using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to save Shopify API credentials for a tenant.
/// Each tenant must create their own Shopify app and provide their credentials.
/// </summary>
[RequirePermission("channels.connect")]
[RequireFeature("channels_management")]
public record SaveShopifyCredentialsCommand : IRequest<Result<ChannelDto>>, ITenantRequest
{
    public Guid? ChannelId { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string ShopDomain { get; init; } = string.Empty;
    public string? Scopes { get; init; }
}

public class SaveShopifyCredentialsCommandHandler : IRequestHandler<SaveShopifyCredentialsCommand, Result<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<SaveShopifyCredentialsCommandHandler> _logger;

    // Default Shopify scopes if not specified
    private const string DefaultScopes = "read_orders,write_orders,read_products,read_inventory,write_inventory,read_locations,read_fulfillments,write_fulfillments";

    public SaveShopifyCredentialsCommandHandler(
        ITenantDbContext dbContext,
        ILogger<SaveShopifyCredentialsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ChannelDto>> Handle(SaveShopifyCredentialsCommand request, CancellationToken cancellationToken)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Result<ChannelDto>.Failure("API Key is required");
        }

        if (string.IsNullOrWhiteSpace(request.ApiSecret))
        {
            return Result<ChannelDto>.Failure("API Secret is required");
        }

        if (string.IsNullOrWhiteSpace(request.ShopDomain))
        {
            return Result<ChannelDto>.Failure("Shop domain is required");
        }

        // Normalize shop domain
        var shopDomain = request.ShopDomain.ToLowerInvariant().Trim();
        if (!shopDomain.EndsWith(".myshopify.com"))
        {
            shopDomain = $"{shopDomain}.myshopify.com";
        }

        Domain.Entities.Channels.SalesChannel channel;

        if (request.ChannelId.HasValue)
        {
            // Update existing channel
            var existingChannel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId.Value, cancellationToken);

            if (existingChannel == null)
            {
                return Result<ChannelDto>.Failure("Channel not found");
            }

            if (existingChannel.Type != ChannelType.Shopify)
            {
                return Result<ChannelDto>.Failure("This channel is not a Shopify channel");
            }

            channel = existingChannel;
        }
        else
        {
            // Check if a Shopify channel with this domain already exists
            var existingByDomain = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Type == ChannelType.Shopify && c.StoreUrl == $"https://{shopDomain}", cancellationToken);

            if (existingByDomain != null)
            {
                if (existingByDomain.IsActive)
                {
                    return Result<ChannelDto>.Failure($"A Shopify channel for {shopDomain} already exists");
                }

                // Reactivate the existing inactive channel
                _logger.LogInformation(
                    "Reactivating existing inactive Shopify channel {ChannelId} for domain {ShopDomain}",
                    existingByDomain.Id, shopDomain);

                existingByDomain.Activate();
                // Clear old access token to force re-authorization with new scopes
                existingByDomain.MarkDisconnected();
                channel = existingByDomain;
            }
            else
            {
                // Create new channel
                channel = Domain.Entities.Channels.SalesChannel.Create(
                    name: $"Shopify - {shopDomain.Replace(".myshopify.com", "")}",
                    type: ChannelType.Shopify,
                    storeUrl: $"https://{shopDomain}",
                    storeName: shopDomain.Replace(".myshopify.com", ""));

                await _dbContext.SalesChannels.AddAsync(channel, cancellationToken);
            }
        }

        // Set credentials
        var scopes = string.IsNullOrWhiteSpace(request.Scopes) ? DefaultScopes : request.Scopes;
        channel.SetApiCredentials(request.ApiKey, request.ApiSecret, scopes);
        channel.SetExternalId(shopDomain);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved Shopify credentials for channel {ChannelId}, shop domain: {ShopDomain}",
            channel.Id, shopDomain);

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
            HasCredentials = !string.IsNullOrEmpty(channel.ApiKey),
            InitialSyncDays = channel.InitialSyncDays,
            InventorySyncDays = channel.InventorySyncDays,
            ProductSyncDays = channel.ProductSyncDays,
            OrderSyncLimit = channel.OrderSyncLimit,
            InventorySyncLimit = channel.InventorySyncLimit,
            ProductSyncLimit = channel.ProductSyncLimit,
            SyncProductsEnabled = channel.SyncProductsEnabled,
            AutoSyncProducts = channel.AutoSyncProducts,
            LastProductSyncAt = channel.LastProductSyncAt,
            LastInventorySyncAt = channel.LastInventorySyncAt
        });
    }
}
