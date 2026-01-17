using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Features.Channels;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Shopify implementation of IChannelSyncService.
/// </summary>
public class ShopifyChannelSyncService : IChannelSyncService
{
    private readonly IShopifyClient _shopifyClient;
    private readonly ShopifyOrderMapper _orderMapper;
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<ShopifyChannelSyncService> _logger;

    public ChannelType ChannelType => ChannelType.Shopify;

    public ShopifyChannelSyncService(
        IShopifyClient shopifyClient,
        ShopifyOrderMapper orderMapper,
        ITenantDbContext dbContext,
        ILogger<ShopifyChannelSyncService> logger)
    {
        _shopifyClient = shopifyClient;
        _orderMapper = orderMapper;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ChannelSyncResult> SyncOrdersAsync(
        Guid channelId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return new ChannelSyncResult
            {
                ChannelId = channelId,
                Status = "Failed",
                Errors = new List<string> { "Channel not found" }
            };
        }

        if (!channel.IsConnected)
        {
            return new ChannelSyncResult
            {
                ChannelId = channelId,
                Status = "Failed",
                Errors = new List<string> { "Channel is not connected" }
            };
        }

        // Use AccessToken for OAuth-based channels
        var accessToken = channel.AccessToken ?? channel.CredentialsEncrypted;
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ChannelSyncResult
            {
                ChannelId = channelId,
                Status = "Failed",
                Errors = new List<string> { "Channel credentials are missing" }
            };
        }

        // Apply InitialSyncDays if no date range specified
        if (!fromDate.HasValue && channel.InitialSyncDays.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-channel.InitialSyncDays.Value);
            _logger.LogInformation("Applying InitialSyncDays={Days}, syncing orders from {FromDate}",
                channel.InitialSyncDays.Value, fromDate);
        }

        var result = new ChannelSyncResult
        {
            ChannelId = channelId,
            SyncedAt = DateTime.UtcNow
        };

        var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

        try
        {
            _logger.LogInformation("Starting order sync for channel {ChannelId}, shop: {ShopDomain}",
                channelId, shopDomain);

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
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Check for pagination
                hasMorePages = orders.Count == 250;
                pageInfo = null; // Would need to extract from response headers for real pagination
            }

            result.Status = result.OrdersFailed > 0 ? "CompletedWithErrors" : "Completed";

            _logger.LogInformation(
                "Completed order sync for channel {ChannelId}: {Imported} imported, {Updated} updated, {Failed} failed",
                channelId, result.OrdersImported, result.OrdersUpdated, result.OrdersFailed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order sync failed for channel {ChannelId}", channelId);
            result.Status = "Failed";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    public async Task<ChannelSyncResult> SyncProductsAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement product sync
        _logger.LogWarning("Product sync not yet implemented for Shopify");
        return new ChannelSyncResult
        {
            ChannelId = channelId,
            Status = "NotImplemented",
            Errors = new List<string> { "Product sync not yet implemented" }
        };
    }

    public async Task<ChannelSyncResult> SyncInventoryAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement inventory sync
        _logger.LogWarning("Inventory sync not yet implemented for Shopify");
        return new ChannelSyncResult
        {
            ChannelId = channelId,
            Status = "NotImplemented",
            Errors = new List<string> { "Inventory sync not yet implemented" }
        };
    }

    private async Task ProcessOrderAsync(
        Guid channelId,
        Models.ShopifyOrder shopifyOrder,
        ChannelSyncResult result,
        CancellationToken cancellationToken)
    {
        var externalOrderId = shopifyOrder.Id.ToString();

        // Check if order already exists
        var existingOrder = await _dbContext.Orders
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
            await _dbContext.Orders.AddAsync(order, cancellationToken);
            result.OrdersImported++;
            _logger.LogDebug("Imported new order {OrderNumber} from Shopify", order.OrderNumber);
        }
    }
}
