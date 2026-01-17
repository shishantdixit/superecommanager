using SuperEcomManager.Application.Features.Channels;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service interface for synchronizing data from sales channels.
/// Implemented per channel type (Shopify, Amazon, etc.).
/// </summary>
public interface IChannelSyncService
{
    /// <summary>
    /// Gets the channel type this service handles.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Syncs orders for a specific channel.
    /// </summary>
    Task<ChannelSyncResult> SyncOrdersAsync(
        Guid channelId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs products for a specific channel.
    /// </summary>
    Task<ChannelSyncResult> SyncProductsAsync(
        Guid channelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs inventory for a specific channel.
    /// </summary>
    Task<ChannelSyncResult> SyncInventoryAsync(
        Guid channelId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for getting the appropriate channel sync service.
/// </summary>
public interface IChannelSyncServiceFactory
{
    /// <summary>
    /// Gets the sync service for a specific channel type.
    /// </summary>
    IChannelSyncService? GetService(ChannelType channelType);
}
