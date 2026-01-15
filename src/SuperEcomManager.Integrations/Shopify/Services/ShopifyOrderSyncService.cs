using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Entities.Orders;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Service for synchronizing orders from Shopify to the internal order system.
/// </summary>
public class ShopifyOrderSyncService : IShopifyOrderSyncService
{
    private readonly IShopifyClient _shopifyClient;
    private readonly ShopifyOrderMapper _orderMapper;
    private readonly ILogger<ShopifyOrderSyncService> _logger;

    // These are injected via property injection since the service is created per-tenant
    public DbSet<SalesChannel>? SalesChannels { get; set; }
    public DbSet<Order>? Orders { get; set; }
    public Func<CancellationToken, Task<int>>? SaveChangesAsync { get; set; }

    public ShopifyOrderSyncService(
        IShopifyClient shopifyClient,
        ShopifyOrderMapper orderMapper,
        ILogger<ShopifyOrderSyncService> logger)
    {
        _shopifyClient = shopifyClient;
        _orderMapper = orderMapper;
        _logger = logger;
    }

    public async Task<OrderSyncResult> SyncOrdersAsync(
        Guid channelId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (SalesChannels == null || Orders == null || SaveChangesAsync == null)
        {
            _logger.LogError("ShopifyOrderSyncService not properly configured with DbContext");
            return OrderSyncResult.Failed("Service not properly configured");
        }

        var channel = await SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return OrderSyncResult.Failed("Channel not found");
        }

        if (!channel.IsActive || string.IsNullOrEmpty(channel.CredentialsEncrypted))
        {
            return OrderSyncResult.Failed("Channel is not active or credentials are missing");
        }

        var result = new OrderSyncResult();
        var shopDomain = channel.StoreUrl!;
        var accessToken = channel.CredentialsEncrypted;

        try
        {
            _logger.LogInformation("Starting order sync for channel {ChannelId}", channelId);

            string? pageInfo = null;
            var hasMorePages = true;
            var totalProcessed = 0;

            while (hasMorePages && totalProcessed < 10000) // Safety limit
            {
                var orders = await _shopifyClient.GetOrdersAsync(
                    shopDomain,
                    accessToken,
                    createdAtMin: fromDate,
                    createdAtMax: toDate,
                    status: "any",
                    limit: 250,
                    pageInfo: pageInfo,
                    cancellationToken: cancellationToken);

                if (orders.Count == 0)
                {
                    hasMorePages = false;
                    continue;
                }

                foreach (var shopifyOrder in orders)
                {
                    try
                    {
                        await ProcessOrderAsync(channel.Id, shopifyOrder, result, cancellationToken);
                        totalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process Shopify order {OrderId}", shopifyOrder.Id);
                        result.OrdersFailed++;
                        result.Errors.Add($"Order {shopifyOrder.Name}: {ex.Message}");
                    }
                }

                // Save in batches
                await SaveChangesAsync(cancellationToken);

                // Check for pagination
                // Note: Shopify uses Link headers for cursor-based pagination
                // For simplicity, we break if we get fewer than 250 orders
                hasMorePages = orders.Count == 250;
                pageInfo = null; // Would need to extract from response headers for real pagination
            }

            channel.RecordSync(true, $"Imported: {result.OrdersImported}, Updated: {result.OrdersUpdated}");
            await SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed order sync for channel {ChannelId}: {Imported} imported, {Updated} updated, {Failed} failed",
                channelId, result.OrdersImported, result.OrdersUpdated, result.OrdersFailed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order sync failed for channel {ChannelId}", channelId);
            channel.RecordSync(false, ex.Message);
            await SaveChangesAsync(cancellationToken);

            result.Status = "Failed";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    public async Task<OrderSyncResult> SyncSingleOrderAsync(
        Guid channelId,
        long shopifyOrderId,
        CancellationToken cancellationToken = default)
    {
        if (SalesChannels == null || Orders == null || SaveChangesAsync == null)
        {
            return OrderSyncResult.Failed("Service not properly configured");
        }

        var channel = await SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return OrderSyncResult.Failed("Channel not found");
        }

        if (!channel.IsActive || string.IsNullOrEmpty(channel.CredentialsEncrypted))
        {
            return OrderSyncResult.Failed("Channel is not active or credentials are missing");
        }

        var result = new OrderSyncResult();

        try
        {
            var shopifyOrder = await _shopifyClient.GetOrderAsync(
                channel.StoreUrl!,
                channel.CredentialsEncrypted,
                shopifyOrderId,
                cancellationToken);

            if (shopifyOrder == null)
            {
                return OrderSyncResult.Failed($"Order {shopifyOrderId} not found in Shopify");
            }

            await ProcessOrderAsync(channel.Id, shopifyOrder, result, cancellationToken);
            await SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync single order {OrderId}", shopifyOrderId);
            result.Status = "Failed";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    private async Task ProcessOrderAsync(
        Guid channelId,
        Models.ShopifyOrder shopifyOrder,
        OrderSyncResult result,
        CancellationToken cancellationToken)
    {
        var externalOrderId = shopifyOrder.Id.ToString();

        // Check if order already exists
        var existingOrder = await Orders!
            .FirstOrDefaultAsync(o =>
                o.ChannelId == channelId &&
                o.ExternalOrderId == externalOrderId,
                cancellationToken);

        if (existingOrder != null)
        {
            // Update existing order
            _orderMapper.UpdateOrder(existingOrder, shopifyOrder);
            result.OrdersUpdated++;
            _logger.LogDebug("Updated order {OrderNumber} from Shopify", existingOrder.OrderNumber);
        }
        else
        {
            // Create new order
            var order = _orderMapper.MapToOrder(shopifyOrder, channelId);
            await Orders.AddAsync(order, cancellationToken);
            result.OrdersImported++;
            _logger.LogDebug("Imported new order {OrderNumber} from Shopify", order.OrderNumber);
        }
    }
}
