using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Service for pushing inventory updates to Shopify.
/// </summary>
public class ShopifyInventorySyncService : IInventorySyncService
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShopifyClient _shopifyClient;
    private readonly ILogger<ShopifyInventorySyncService> _logger;

    public ChannelType ChannelType => ChannelType.Shopify;

    public ShopifyInventorySyncService(
        ITenantDbContext dbContext,
        IShopifyClient shopifyClient,
        ILogger<ShopifyInventorySyncService> logger)
    {
        _dbContext = dbContext;
        _shopifyClient = shopifyClient;
        _logger = logger;
    }

    public async Task<InventoryPushResult> PushInventoryAsync(
        Guid channelId,
        string sku,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get channel
            var channel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

            if (channel == null)
            {
                return InventoryPushResult.Failed(sku, "Channel not found");
            }

            if (!channel.IsConnected || string.IsNullOrEmpty(channel.AccessToken))
            {
                return InventoryPushResult.Failed(sku, "Channel is not connected");
            }

            var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

            // Get locations
            var locations = await _shopifyClient.GetLocationsAsync(shopDomain, channel.AccessToken, cancellationToken);
            var primaryLocation = locations.FirstOrDefault(l => l.Active) ?? locations.FirstOrDefault();

            if (primaryLocation == null)
            {
                return InventoryPushResult.Failed(sku, "No locations found in Shopify store");
            }

            // Find the inventory item ID for this SKU
            var inventoryItemId = await FindInventoryItemIdAsync(shopDomain, channel.AccessToken, sku, cancellationToken);

            if (inventoryItemId == null)
            {
                return InventoryPushResult.Failed(sku, $"SKU '{sku}' not found in Shopify");
            }

            // Set inventory level
            var result = await _shopifyClient.SetInventoryLevelAsync(
                shopDomain,
                channel.AccessToken,
                inventoryItemId.Value,
                primaryLocation.Id,
                quantity,
                cancellationToken);

            if (result != null)
            {
                _logger.LogInformation("Pushed inventory for SKU {Sku} to Shopify: {Quantity}", sku, quantity);
                return InventoryPushResult.Succeeded(sku, quantity);
            }

            return InventoryPushResult.Failed(sku, "Failed to update inventory in Shopify");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push inventory for SKU {Sku} to Shopify", sku);
            return InventoryPushResult.Failed(sku, ex.Message);
        }
    }

    public async Task<List<InventoryPushResult>> PushInventoryBatchAsync(
        Guid channelId,
        Dictionary<string, int> inventoryUpdates,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InventoryPushResult>();

        if (inventoryUpdates.Count == 0)
        {
            return results;
        }

        try
        {
            // Get channel
            var channel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

            if (channel == null)
            {
                return inventoryUpdates.Keys.Select(sku => InventoryPushResult.Failed(sku, "Channel not found")).ToList();
            }

            if (!channel.IsConnected || string.IsNullOrEmpty(channel.AccessToken))
            {
                return inventoryUpdates.Keys.Select(sku => InventoryPushResult.Failed(sku, "Channel is not connected")).ToList();
            }

            var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

            // Get locations
            var locations = await _shopifyClient.GetLocationsAsync(shopDomain, channel.AccessToken, cancellationToken);
            var primaryLocation = locations.FirstOrDefault(l => l.Active) ?? locations.FirstOrDefault();

            if (primaryLocation == null)
            {
                return inventoryUpdates.Keys.Select(sku => InventoryPushResult.Failed(sku, "No locations found")).ToList();
            }

            // Build SKU to inventory item ID mapping
            var skuToInventoryItemId = await BuildSkuInventoryMapAsync(
                shopDomain, channel.AccessToken, inventoryUpdates.Keys.ToList(), cancellationToken);

            // Push updates
            foreach (var (sku, quantity) in inventoryUpdates)
            {
                try
                {
                    if (!skuToInventoryItemId.TryGetValue(sku.ToUpperInvariant(), out var inventoryItemId))
                    {
                        results.Add(InventoryPushResult.Failed(sku, $"SKU '{sku}' not found in Shopify"));
                        continue;
                    }

                    var result = await _shopifyClient.SetInventoryLevelAsync(
                        shopDomain,
                        channel.AccessToken,
                        inventoryItemId,
                        primaryLocation.Id,
                        quantity,
                        cancellationToken);

                    if (result != null)
                    {
                        results.Add(InventoryPushResult.Succeeded(sku, quantity));
                        _logger.LogDebug("Pushed inventory for SKU {Sku}: {Quantity}", sku, quantity);
                    }
                    else
                    {
                        results.Add(InventoryPushResult.Failed(sku, "Failed to update inventory"));
                    }
                }
                catch (Exception ex)
                {
                    results.Add(InventoryPushResult.Failed(sku, ex.Message));
                    _logger.LogWarning(ex, "Failed to push inventory for SKU {Sku}", sku);
                }
            }

            _logger.LogInformation(
                "Batch inventory push completed: {Succeeded} succeeded, {Failed} failed",
                results.Count(r => r.Success),
                results.Count(r => !r.Success));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch inventory push failed for channel {ChannelId}", channelId);
            return inventoryUpdates.Keys.Select(sku => InventoryPushResult.Failed(sku, ex.Message)).ToList();
        }
    }

    private async Task<long?> FindInventoryItemIdAsync(
        string shopDomain,
        string accessToken,
        string sku,
        CancellationToken cancellationToken)
    {
        // Search through products to find the SKU
        string? pageInfo = null;
        var hasMorePages = true;

        while (hasMorePages)
        {
            var products = await _shopifyClient.GetProductsAsync(
                shopDomain, accessToken, limit: 250, pageInfo: pageInfo, cancellationToken: cancellationToken);

            if (products.Count == 0)
            {
                hasMorePages = false;
                continue;
            }

            foreach (var product in products)
            {
                var variant = product.Variants.FirstOrDefault(v =>
                    !string.IsNullOrEmpty(v.Sku) &&
                    v.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

                if (variant != null && variant.InventoryItemId > 0)
                {
                    return variant.InventoryItemId;
                }
            }

            hasMorePages = products.Count == 250;
            pageInfo = null; // Would need to extract from response headers
        }

        return null;
    }

    private async Task<Dictionary<string, long>> BuildSkuInventoryMapAsync(
        string shopDomain,
        string accessToken,
        List<string> skus,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var skuSet = new HashSet<string>(skus.Select(s => s.ToUpperInvariant()));

        string? pageInfo = null;
        var hasMorePages = true;

        while (hasMorePages && skuSet.Count > 0)
        {
            var products = await _shopifyClient.GetProductsAsync(
                shopDomain, accessToken, limit: 250, pageInfo: pageInfo, cancellationToken: cancellationToken);

            if (products.Count == 0)
            {
                hasMorePages = false;
                continue;
            }

            foreach (var product in products)
            {
                foreach (var variant in product.Variants)
                {
                    if (!string.IsNullOrEmpty(variant.Sku) && variant.InventoryItemId > 0)
                    {
                        var normalizedSku = variant.Sku.Trim().ToUpperInvariant();
                        if (skuSet.Contains(normalizedSku) && !map.ContainsKey(normalizedSku))
                        {
                            map[normalizedSku] = variant.InventoryItemId;
                            skuSet.Remove(normalizedSku);
                        }
                    }
                }
            }

            hasMorePages = products.Count == 250;
            pageInfo = null;
        }

        return map;
    }
}
