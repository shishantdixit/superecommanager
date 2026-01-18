using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to update channel sync settings.
/// </summary>
[RequirePermission("channels.settings")]
[RequireFeature("channels_management")]
public record UpdateChannelSettingsCommand : IRequest<Result<ChannelDto>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
    public bool? AutoSyncOrders { get; init; }
    public bool? AutoSyncInventory { get; init; }

    // Advanced sync settings
    public int? InitialSyncDays { get; init; }
    public int? InventorySyncDays { get; init; }
    public int? ProductSyncDays { get; init; }
    public int? OrderSyncLimit { get; init; }
    public int? InventorySyncLimit { get; init; }
    public int? ProductSyncLimit { get; init; }
    public bool? SyncProductsEnabled { get; init; }
    public bool? AutoSyncProducts { get; init; }
}

public class UpdateChannelSettingsCommandHandler : IRequestHandler<UpdateChannelSettingsCommand, Result<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateChannelSettingsCommandHandler> _logger;

    public UpdateChannelSettingsCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateChannelSettingsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ChannelDto>> Handle(UpdateChannelSettingsCommand request, CancellationToken cancellationToken)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<ChannelDto>.Failure("Channel not found");
        }

        if (!channel.IsActive)
        {
            return Result<ChannelDto>.Failure("Cannot update settings for an inactive channel");
        }

        // Update basic sync settings
        var autoSyncOrders = request.AutoSyncOrders ?? channel.AutoSyncOrders;
        var autoSyncInventory = request.AutoSyncInventory ?? channel.AutoSyncInventory;
        channel.UpdateSyncSettings(autoSyncOrders, autoSyncInventory);

        // Update advanced sync settings if provided
        if (request.InitialSyncDays.HasValue || request.InventorySyncDays.HasValue ||
            request.ProductSyncDays.HasValue || request.OrderSyncLimit.HasValue ||
            request.InventorySyncLimit.HasValue || request.ProductSyncLimit.HasValue ||
            request.SyncProductsEnabled.HasValue || request.AutoSyncProducts.HasValue)
        {
            var initialSyncDays = request.InitialSyncDays ?? channel.InitialSyncDays;
            var inventorySyncDays = request.InventorySyncDays ?? channel.InventorySyncDays;
            var productSyncDays = request.ProductSyncDays ?? channel.ProductSyncDays;
            var orderSyncLimit = request.OrderSyncLimit ?? channel.OrderSyncLimit;
            var inventorySyncLimit = request.InventorySyncLimit ?? channel.InventorySyncLimit;
            var productSyncLimit = request.ProductSyncLimit ?? channel.ProductSyncLimit;
            var syncProductsEnabled = request.SyncProductsEnabled ?? channel.SyncProductsEnabled;
            var autoSyncProducts = request.AutoSyncProducts ?? channel.AutoSyncProducts;
            channel.UpdateAdvancedSyncSettings(
                initialSyncDays, inventorySyncDays, productSyncDays,
                orderSyncLimit, inventorySyncLimit, productSyncLimit,
                syncProductsEnabled, autoSyncProducts);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated channel {ChannelId} settings: AutoSyncOrders={AutoSyncOrders}, AutoSyncInventory={AutoSyncInventory}",
            channel.Id, autoSyncOrders, autoSyncInventory);

        // Get order count for DTO
        var orderCount = await _dbContext.Orders
            .CountAsync(o => o.ChannelId == channel.Id, cancellationToken);

        return Result<ChannelDto>.Success(new ChannelDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Type = channel.Type,
            IsActive = channel.IsActive,
            StoreUrl = channel.StoreUrl,
            StoreName = channel.StoreName,
            LastSyncAt = channel.LastSyncAt,
            TotalOrders = orderCount,
            SyncStatus = MapSyncStatus(channel.LastSyncStatus),
            CreatedAt = channel.CreatedAt,
            AutoSyncOrders = channel.AutoSyncOrders,
            AutoSyncInventory = channel.AutoSyncInventory,
            IsConnected = channel.IsConnected,
            HasCredentials = channel.ApiKey != null,
            LastError = channel.LastError,
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

    private static ChannelSyncStatus MapSyncStatus(string? lastSyncStatus)
    {
        if (string.IsNullOrEmpty(lastSyncStatus))
            return ChannelSyncStatus.NotStarted;

        return lastSyncStatus.StartsWith("Success", StringComparison.OrdinalIgnoreCase)
            ? ChannelSyncStatus.Completed
            : ChannelSyncStatus.Failed;
    }
}
