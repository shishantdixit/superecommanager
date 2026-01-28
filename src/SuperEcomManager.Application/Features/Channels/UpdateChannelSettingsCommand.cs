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

        // Update advanced sync settings
        // Note: We pass the request values directly to allow setting null (for "All time" / "Unlimited")
        // The frontend explicitly sends null when user selects "All time" or "Unlimited"
        var syncProductsEnabled = request.SyncProductsEnabled ?? channel.SyncProductsEnabled;
        var autoSyncProducts = request.AutoSyncProducts ?? channel.AutoSyncProducts;

        _logger.LogInformation(
            "Updating channel settings - InitialSyncDays: {InitialSyncDays}, OrderSyncLimit: {OrderSyncLimit}, ProductSyncDays: {ProductSyncDays}, ProductSyncLimit: {ProductSyncLimit}",
            request.InitialSyncDays?.ToString() ?? "null (all time)",
            request.OrderSyncLimit?.ToString() ?? "null (unlimited)",
            request.ProductSyncDays?.ToString() ?? "null (all time)",
            request.ProductSyncLimit?.ToString() ?? "null (unlimited)");

        channel.UpdateAdvancedSyncSettings(
            request.InitialSyncDays,
            request.InventorySyncDays,
            request.ProductSyncDays,
            request.OrderSyncLimit,
            request.InventorySyncLimit,
            request.ProductSyncLimit,
            syncProductsEnabled,
            autoSyncProducts);

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
