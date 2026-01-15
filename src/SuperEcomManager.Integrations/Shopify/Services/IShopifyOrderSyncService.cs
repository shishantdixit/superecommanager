namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Service for synchronizing orders from Shopify.
/// </summary>
public interface IShopifyOrderSyncService
{
    /// <summary>
    /// Syncs orders for a specific channel.
    /// </summary>
    Task<OrderSyncResult> SyncOrdersAsync(
        Guid channelId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a single order by its Shopify ID.
    /// </summary>
    Task<OrderSyncResult> SyncSingleOrderAsync(
        Guid channelId,
        long shopifyOrderId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an order sync operation.
/// </summary>
public class OrderSyncResult
{
    public int OrdersImported { get; set; }
    public int OrdersUpdated { get; set; }
    public int OrdersFailed { get; set; }
    public int OrdersSkipped { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public List<string> Errors { get; set; } = new();

    public static OrderSyncResult Success(int imported, int updated, int skipped = 0)
    {
        return new OrderSyncResult
        {
            OrdersImported = imported,
            OrdersUpdated = updated,
            OrdersSkipped = skipped,
            Status = "Completed"
        };
    }

    public static OrderSyncResult Failed(string error)
    {
        return new OrderSyncResult
        {
            Status = "Failed",
            Errors = new List<string> { error }
        };
    }
}
