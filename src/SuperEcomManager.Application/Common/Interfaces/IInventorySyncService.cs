using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for pushing inventory updates to external sales channels.
/// </summary>
public interface IInventorySyncService
{
    /// <summary>
    /// Gets the channel type this service handles.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Pushes inventory level for a specific SKU to the external channel.
    /// </summary>
    /// <param name="channelId">The channel to push to.</param>
    /// <param name="sku">The SKU to update.</param>
    /// <param name="quantity">The new quantity available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the push operation.</returns>
    Task<InventoryPushResult> PushInventoryAsync(
        Guid channelId,
        string sku,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes inventory levels for multiple SKUs to the external channel.
    /// </summary>
    /// <param name="channelId">The channel to push to.</param>
    /// <param name="inventoryUpdates">Dictionary of SKU to quantity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results of the push operations.</returns>
    Task<List<InventoryPushResult>> PushInventoryBatchAsync(
        Guid channelId,
        Dictionary<string, int> inventoryUpdates,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an inventory push operation.
/// </summary>
public class InventoryPushResult
{
    public string Sku { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int? QuantityPushed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static InventoryPushResult Succeeded(string sku, int quantity)
        => new()
        {
            Sku = sku,
            Success = true,
            QuantityPushed = quantity
        };

    public static InventoryPushResult Failed(string sku, string errorMessage)
        => new()
        {
            Sku = sku,
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Factory for getting the appropriate inventory sync service.
/// </summary>
public interface IInventorySyncServiceFactory
{
    /// <summary>
    /// Gets the inventory sync service for a specific channel type.
    /// </summary>
    IInventorySyncService? GetService(ChannelType channelType);

    /// <summary>
    /// Checks if inventory sync is supported for the given channel type.
    /// </summary>
    bool IsSupported(ChannelType channelType);
}
