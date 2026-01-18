using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to save Shopify access token directly for Custom Apps.
/// Custom Apps created in Shopify Admin provide access tokens directly without OAuth.
/// </summary>
[RequirePermission("channels.connect")]
[RequireFeature("channels_management")]
public record SaveShopifyAccessTokenCommand : IRequest<Result<ChannelDto>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
}

public class SaveShopifyAccessTokenCommandHandler : IRequestHandler<SaveShopifyAccessTokenCommand, Result<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IChannelSyncServiceFactory _syncServiceFactory;
    private readonly ILogger<SaveShopifyAccessTokenCommandHandler> _logger;

    public SaveShopifyAccessTokenCommandHandler(
        ITenantDbContext dbContext,
        IChannelSyncServiceFactory syncServiceFactory,
        ILogger<SaveShopifyAccessTokenCommandHandler> logger)
    {
        _dbContext = dbContext;
        _syncServiceFactory = syncServiceFactory;
        _logger = logger;
    }

    public async Task<Result<ChannelDto>> Handle(SaveShopifyAccessTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return Result<ChannelDto>.Failure("Access token is required");
        }

        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<ChannelDto>.Failure("Channel not found");
        }

        if (channel.Type != ChannelType.Shopify)
        {
            return Result<ChannelDto>.Failure("This channel is not a Shopify channel");
        }

        // Set the access token directly
        channel.SetAccessToken(request.AccessToken);
        channel.MarkConnected();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved direct access token for Shopify channel {ChannelId}",
            channel.Id);

        // Verify the token works by trying to get locations
        try
        {
            var syncService = _syncServiceFactory.GetService(ChannelType.Shopify);
            if (syncService != null)
            {
                var locations = await syncService.GetLocationsAsync(channel.Id, cancellationToken);
                _logger.LogInformation(
                    "Verified access token for channel {ChannelId}, found {LocationCount} locations",
                    channel.Id, locations.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not verify access token for channel {ChannelId}, but token was saved",
                channel.Id);
        }

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
