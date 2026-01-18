using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Features.Channels;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Entities.Inventory;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Shopify.Models;

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

        var result = new ChannelSyncResult
        {
            ChannelId = channelId,
            SyncedAt = DateTime.UtcNow
        };

        var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

        // Apply sync limits
        var productSyncLimit = channel.ProductSyncLimit ?? 10000; // Default to 10000 if not set
        DateTime? updatedAtMin = null;
        if (channel.ProductSyncDays.HasValue)
        {
            updatedAtMin = DateTime.UtcNow.AddDays(-channel.ProductSyncDays.Value);
            _logger.LogInformation("Applying ProductSyncDays={Days}, syncing products updated since {UpdatedAtMin}",
                channel.ProductSyncDays.Value, updatedAtMin);
        }

        try
        {
            _logger.LogInformation("Starting product sync for channel {ChannelId}, shop: {ShopDomain}, limit: {Limit}",
                channelId, shopDomain, productSyncLimit);

            // Get existing products by SKU for update detection
            // Include soft-deleted products to avoid unique constraint violations
            var existingProducts = await _dbContext.Products
                .IgnoreQueryFilters()
                .ToDictionaryAsync(p => p.Sku, p => p, cancellationToken);

            var existingVariants = await _dbContext.ProductVariants
                .IgnoreQueryFilters()
                .ToDictionaryAsync(v => v.Sku, v => v, cancellationToken);

            var existingInventory = await _dbContext.Inventory
                .Where(i => !string.IsNullOrEmpty(i.Sku))
                .ToDictionaryAsync(i => i.Sku, i => i, cancellationToken);

            string? pageInfo = null;
            var hasMorePages = true;
            var totalProcessed = 0;

            while (hasMorePages && totalProcessed < productSyncLimit)
            {
                var batchSize = Math.Min(250, productSyncLimit - totalProcessed);
                var products = await _shopifyClient.GetProductsAsync(
                    shopDomain,
                    accessToken,
                    limit: batchSize,
                    pageInfo: pageInfo,
                    updatedAtMin: updatedAtMin,
                    cancellationToken: cancellationToken);

                if (products.Count == 0)
                {
                    hasMorePages = false;
                    continue;
                }

                foreach (var shopifyProduct in products)
                {
                    try
                    {
                        await ProcessProductAsync(
                            shopifyProduct,
                            existingProducts,
                            existingVariants,
                            existingInventory,
                            result,
                            cancellationToken);
                        totalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process Shopify product {ProductId}: {Title}",
                            shopifyProduct.Id, shopifyProduct.Title);
                        result.ProductsFailed++;
                        result.Errors.Add($"Product {shopifyProduct.Title}: {ex.Message}");
                    }
                }

                // Save in batches
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Check if we've reached the limit or if there are more products
                hasMorePages = products.Count == batchSize && totalProcessed < productSyncLimit;
                pageInfo = null; // Would need to extract from response headers for real pagination
            }

            result.Status = result.ProductsFailed > 0 ? "CompletedWithErrors" : "Completed";

            _logger.LogInformation(
                "Completed product sync for channel {ChannelId}: {Imported} imported, {Updated} updated, {Failed} failed",
                channelId, result.ProductsImported, result.ProductsUpdated, result.ProductsFailed);

            return result;
        }
        catch (Exception ex)
        {
            // Log with inner exception details for better debugging
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError(ex, "Product sync failed for channel {ChannelId}. Inner: {InnerMessage}",
                channelId, innerMessage);
            result.Status = "Failed";
            result.Errors.Add($"{ex.Message} - Inner: {innerMessage}");
            return result;
        }
    }

    private async Task ProcessProductAsync(
        Models.ShopifyProduct shopifyProduct,
        Dictionary<string, Product> existingProducts,
        Dictionary<string, ProductVariant> existingVariants,
        Dictionary<string, InventoryItem> existingInventory,
        ChannelSyncResult result,
        CancellationToken cancellationToken)
    {
        // Get the main image URL
        var mainImageUrl = shopifyProduct.Image?.Src ?? shopifyProduct.Images.FirstOrDefault()?.Src;

        // Check if this is a simple product (single variant with "Default Title")
        var isSimpleProduct = shopifyProduct.Variants.Count == 1 &&
            (shopifyProduct.Variants[0].Title == "Default Title" ||
             string.IsNullOrEmpty(shopifyProduct.Variants[0].Option1));

        if (isSimpleProduct)
        {
            // Simple product - use the variant SKU as the product SKU
            var variant = shopifyProduct.Variants[0];
            var sku = variant.Sku?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(sku))
            {
                // Generate SKU from Shopify product ID if not set
                sku = $"SHOPIFY-{shopifyProduct.Id}";
            }

            if (existingProducts.TryGetValue(sku, out var existingProduct))
            {
                // Restore soft-deleted product if found
                if (existingProduct.DeletedAt.HasValue)
                {
                    existingProduct.DeletedAt = null;
                    existingProduct.DeletedBy = null;
                    _logger.LogInformation("Restored soft-deleted product {Sku}", sku);
                }

                // Update existing product (truncate fields to fit DB limits)
                existingProduct.Update(
                    Truncate(shopifyProduct.Title, 500)!,
                    StripHtml(shopifyProduct.BodyHtml),
                    Truncate(shopifyProduct.ProductType, 200),
                    Truncate(shopifyProduct.Vendor, 200));

                if (decimal.TryParse(variant.Price, out var price))
                {
                    existingProduct.UpdatePricing(
                        existingProduct.CostPrice, // Keep existing cost price
                        new Domain.ValueObjects.Money(price, "INR"));
                }

                if (mainImageUrl != null)
                {
                    existingProduct.SetImageUrl(Truncate(mainImageUrl, 1000));
                }

                if (variant.Weight > 0)
                {
                    var weightInKg = ConvertWeightToKg(variant.Weight, variant.WeightUnit);
                    existingProduct.SetWeight(weightInKg);
                }

                result.ProductsUpdated++;
                _logger.LogDebug("Updated product {Sku}: {Title}", sku, shopifyProduct.Title);
            }
            else
            {
                // Create new product (truncate fields to fit DB limits)
                var sellingPrice = decimal.TryParse(variant.Price, out var p)
                    ? new Domain.ValueObjects.Money(p, "INR")
                    : Domain.ValueObjects.Money.Zero;

                var truncatedTitle = Truncate(shopifyProduct.Title, 500)!;

                var product = Product.Create(
                    sku,
                    truncatedTitle,
                    Domain.ValueObjects.Money.Zero, // Cost price unknown from Shopify
                    sellingPrice);

                product.Update(
                    truncatedTitle,
                    StripHtml(shopifyProduct.BodyHtml),
                    Truncate(shopifyProduct.ProductType, 200),
                    Truncate(shopifyProduct.Vendor, 200));

                if (mainImageUrl != null)
                {
                    product.SetImageUrl(Truncate(mainImageUrl, 1000));
                }

                if (variant.Weight > 0)
                {
                    var weightInKg = ConvertWeightToKg(variant.Weight, variant.WeightUnit);
                    product.SetWeight(weightInKg);
                }

                await _dbContext.Products.AddAsync(product, cancellationToken);
                existingProducts[sku] = product;

                // Create inventory item if it doesn't exist
                if (!existingInventory.ContainsKey(sku))
                {
                    var inventoryItem = InventoryItem.Create(product.Id, sku);
                    inventoryItem.SetQuantityOnHand(variant.InventoryQuantity);
                    await _dbContext.Inventory.AddAsync(inventoryItem, cancellationToken);
                    existingInventory[sku] = inventoryItem;
                }

                result.ProductsImported++;
                _logger.LogDebug("Imported new product {Sku}: {Title}", sku, shopifyProduct.Title);
            }
        }
        else
        {
            // Multi-variant product - create parent product and variants
            // Use Shopify product ID to ensure unique parent SKU
            var parentSku = $"SHOPIFY-P{shopifyProduct.Id}";

            Product parentProduct;
            var isNewProduct = false;

            if (existingProducts.TryGetValue(parentSku, out var existing))
            {
                parentProduct = existing;

                // Restore soft-deleted product if found
                if (existing.DeletedAt.HasValue)
                {
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                    _logger.LogInformation("Restored soft-deleted product {Sku}", parentSku);
                }

                // Update existing product (truncate fields to fit DB limits)
                var truncatedTitle = Truncate(shopifyProduct.Title, 500)!;
                existing.Update(
                    truncatedTitle,
                    StripHtml(shopifyProduct.BodyHtml),
                    Truncate(shopifyProduct.ProductType, 200),
                    Truncate(shopifyProduct.Vendor, 200));

                if (mainImageUrl != null)
                {
                    existing.SetImageUrl(Truncate(mainImageUrl, 1000));
                }

                result.ProductsUpdated++;
            }
            else
            {
                // Create new product (truncate fields to fit DB limits)
                var truncatedTitle = Truncate(shopifyProduct.Title, 500)!;

                // Get average price from variants
                var avgPrice = shopifyProduct.Variants
                    .Select(v => decimal.TryParse(v.Price, out var p) ? p : 0)
                    .Where(p => p > 0)
                    .DefaultIfEmpty(0)
                    .Average();

                parentProduct = Product.Create(
                    parentSku,
                    truncatedTitle,
                    Domain.ValueObjects.Money.Zero,
                    new Domain.ValueObjects.Money(avgPrice, "INR"));

                parentProduct.Update(
                    truncatedTitle,
                    StripHtml(shopifyProduct.BodyHtml),
                    Truncate(shopifyProduct.ProductType, 200),
                    Truncate(shopifyProduct.Vendor, 200));

                if (mainImageUrl != null)
                {
                    parentProduct.SetImageUrl(Truncate(mainImageUrl, 1000));
                }

                await _dbContext.Products.AddAsync(parentProduct, cancellationToken);
                existingProducts[parentSku] = parentProduct;
                isNewProduct = true;
                result.ProductsImported++;
            }

            // Process variants (truncate fields to fit DB limits)
            var option1Name = Truncate(shopifyProduct.Options.ElementAtOrDefault(0)?.Name, 100);
            var option2Name = Truncate(shopifyProduct.Options.ElementAtOrDefault(1)?.Name, 100);

            foreach (var shopifyVariant in shopifyProduct.Variants)
            {
                var variantSku = shopifyVariant.Sku?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(variantSku))
                {
                    variantSku = $"SHOPIFY-{shopifyVariant.Id}";
                }
                // Truncate SKU to 100 chars
                variantSku = Truncate(variantSku, 100)!;

                // Get variant image
                string? variantImageUrl = null;
                if (shopifyVariant.ImageId.HasValue)
                {
                    variantImageUrl = Truncate(
                        shopifyProduct.Images.FirstOrDefault(i => i.Id == shopifyVariant.ImageId.Value)?.Src,
                        1000);
                }

                var truncatedVariantTitle = Truncate(shopifyVariant.Title, 500)!;
                var truncatedOption1 = Truncate(shopifyVariant.Option1, 200);
                var truncatedOption2 = Truncate(shopifyVariant.Option2, 200);

                if (existingVariants.TryGetValue(variantSku, out var existingVariant))
                {
                    // Update existing variant
                    existingVariant.Update(truncatedVariantTitle);
                    existingVariant.SetOptions(option1Name, truncatedOption1, option2Name, truncatedOption2);

                    if (decimal.TryParse(shopifyVariant.Price, out var price))
                    {
                        existingVariant.SetPricing(
                            existingVariant.CostPrice, // Keep existing cost
                            new Domain.ValueObjects.Money(price, "INR"));
                    }

                    if (variantImageUrl != null)
                    {
                        existingVariant.SetImageUrl(variantImageUrl);
                    }

                    if (shopifyVariant.Weight > 0)
                    {
                        existingVariant.SetWeight(ConvertWeightToKg(shopifyVariant.Weight, shopifyVariant.WeightUnit));
                    }
                }
                else
                {
                    // Create new variant
                    var variant = ProductVariant.Create(
                        parentProduct.Id,
                        variantSku,
                        truncatedVariantTitle);

                    variant.SetOptions(option1Name, truncatedOption1, option2Name, truncatedOption2);

                    if (decimal.TryParse(shopifyVariant.Price, out var price))
                    {
                        variant.SetPricing(null, new Domain.ValueObjects.Money(price, "INR"));
                    }

                    if (variantImageUrl != null)
                    {
                        variant.SetImageUrl(variantImageUrl);
                    }

                    if (shopifyVariant.Weight > 0)
                    {
                        variant.SetWeight(ConvertWeightToKg(shopifyVariant.Weight, shopifyVariant.WeightUnit));
                    }

                    parentProduct.AddVariant(variant);
                    await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);
                    existingVariants[variantSku] = variant;

                    // Create inventory item for variant
                    if (!existingInventory.ContainsKey(variantSku))
                    {
                        var inventoryItem = InventoryItem.Create(parentProduct.Id, variantSku, variant.Id);
                        inventoryItem.SetQuantityOnHand(shopifyVariant.InventoryQuantity);
                        await _dbContext.Inventory.AddAsync(inventoryItem, cancellationToken);
                        existingInventory[variantSku] = inventoryItem;
                    }
                }
            }

            if (isNewProduct)
            {
                _logger.LogDebug("Imported new product with {VariantCount} variants: {Title}",
                    shopifyProduct.Variants.Count, shopifyProduct.Title);
            }
            else
            {
                _logger.LogDebug("Updated product with {VariantCount} variants: {Title}",
                    shopifyProduct.Variants.Count, shopifyProduct.Title);
            }
        }
    }

    private static string? StripHtml(string? html, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        // Simple HTML tag stripping - could use HtmlAgilityPack for more robust handling
        var stripped = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "").Trim();

        return Truncate(stripped, maxLength);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }

    private static decimal ConvertWeightToKg(decimal weight, string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "g" => weight / 1000m,
            "lb" => weight * 0.453592m,
            "oz" => weight * 0.0283495m,
            _ => weight // Assume kg
        };
    }

    public async Task<ChannelSyncResult> SyncInventoryAsync(
        Guid channelId,
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

        var result = new ChannelSyncResult
        {
            ChannelId = channelId,
            SyncedAt = DateTime.UtcNow
        };

        var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

        try
        {
            // Calculate updatedAtMin based on InventorySyncDays setting
            DateTime? updatedAtMin = null;
            if (channel.InventorySyncDays.HasValue)
            {
                updatedAtMin = DateTime.UtcNow.AddDays(-channel.InventorySyncDays.Value);
                _logger.LogInformation("Applying InventorySyncDays={Days}, syncing products updated since {UpdatedAtMin}",
                    channel.InventorySyncDays.Value, updatedAtMin);
            }

            _logger.LogInformation("Starting inventory sync for channel {ChannelId}, shop: {ShopDomain}",
                channelId, shopDomain);

            // Get locations first
            var locations = await _shopifyClient.GetLocationsAsync(shopDomain, accessToken, cancellationToken);
            if (locations.Count == 0)
            {
                return new ChannelSyncResult
                {
                    ChannelId = channelId,
                    Status = "Failed",
                    Errors = new List<string> { "No locations found in Shopify store" }
                };
            }

            // Use the first active location (primary location)
            var primaryLocation = locations.FirstOrDefault(l => l.Active) ?? locations.First();
            _logger.LogInformation("Using location {LocationName} (ID: {LocationId}) for inventory sync",
                primaryLocation.Name, primaryLocation.Id);

            // Get all local inventory items with their SKUs
            var localInventory = await _dbContext.Inventory
                .Include(i => i.Product)
                .Where(i => !string.IsNullOrEmpty(i.Sku))
                .ToDictionaryAsync(i => i.Sku.ToUpperInvariant(), i => i, cancellationToken);

            _logger.LogInformation("Found {Count} local inventory items to sync", localInventory.Count);

            // Fetch products from Shopify in batches
            string? pageInfo = null;
            var hasMorePages = true;
            var totalProcessed = 0;
            var inventoryItemIds = new List<long>();
            var skuToInventoryItemId = new Dictionary<string, long>();

            // First pass: collect all inventory item IDs from products
            while (hasMorePages && totalProcessed < 10000)
            {
                var products = await _shopifyClient.GetProductsAsync(
                    shopDomain,
                    accessToken,
                    limit: 250,
                    pageInfo: pageInfo,
                    updatedAtMin: updatedAtMin,
                    cancellationToken: cancellationToken);

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
                            if (!skuToInventoryItemId.ContainsKey(normalizedSku))
                            {
                                skuToInventoryItemId[normalizedSku] = variant.InventoryItemId;
                                inventoryItemIds.Add(variant.InventoryItemId);
                            }
                        }
                        totalProcessed++;
                    }
                }

                hasMorePages = products.Count == 250;
                pageInfo = null; // Would need to extract from response headers
            }

            _logger.LogInformation("Found {Count} products with inventory tracking in Shopify", skuToInventoryItemId.Count);

            // Fetch inventory levels in batches
            var shopifyInventoryLevels = new Dictionary<long, int>();
            var batchSize = 50; // Shopify API limit for inventory levels

            for (var i = 0; i < inventoryItemIds.Count; i += batchSize)
            {
                var batch = inventoryItemIds.Skip(i).Take(batchSize).ToArray();
                var levels = await _shopifyClient.GetInventoryLevelsAsync(
                    shopDomain,
                    accessToken,
                    batch,
                    new[] { primaryLocation.Id },
                    cancellationToken);

                foreach (var level in levels)
                {
                    shopifyInventoryLevels[level.InventoryItemId] = level.Available ?? 0;
                }
            }

            _logger.LogInformation("Fetched {Count} inventory levels from Shopify", shopifyInventoryLevels.Count);

            // Update local inventory
            foreach (var (sku, inventoryItemId) in skuToInventoryItemId)
            {
                try
                {
                    if (!shopifyInventoryLevels.TryGetValue(inventoryItemId, out var shopifyQuantity))
                    {
                        result.InventorySkipped++;
                        continue;
                    }

                    if (localInventory.TryGetValue(sku, out var localItem))
                    {
                        // Update existing inventory
                        if (localItem.QuantityOnHand != shopifyQuantity)
                        {
                            var oldQty = localItem.QuantityOnHand;
                            localItem.SetQuantityOnHand(shopifyQuantity);
                            localItem.SetLocation(primaryLocation.Name);

                            // Record stock movement for audit
                            var movement = StockMovement.Create(
                                localItem.Id,
                                localItem.Sku,
                                Domain.Enums.MovementType.Adjustment,
                                shopifyQuantity - oldQty,
                                oldQty,
                                shopifyQuantity,
                                userId: null,
                                referenceType: "SalesChannel",
                                referenceId: channelId.ToString(),
                                notes: $"Synced from Shopify location: {primaryLocation.Name}");

                            await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
                            result.InventoryUpdated++;

                            _logger.LogDebug("Updated inventory for SKU {Sku}: {OldQty} -> {NewQty}",
                                sku, oldQty, shopifyQuantity);
                        }
                        else
                        {
                            result.InventorySkipped++;
                        }
                    }
                    else
                    {
                        // SKU not found in local inventory
                        result.InventorySkipped++;
                        _logger.LogDebug("SKU {Sku} not found in local inventory, skipping", sku);
                    }
                }
                catch (Exception ex)
                {
                    result.InventoryFailed++;
                    result.Errors.Add($"SKU {sku}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to sync inventory for SKU {Sku}", sku);
                }
            }

            // Save changes
            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Status = result.InventoryFailed > 0 ? "CompletedWithErrors" : "Completed";

            _logger.LogInformation(
                "Completed inventory sync for channel {ChannelId}: {Updated} updated, {Skipped} skipped, {Failed} failed",
                channelId, result.InventoryUpdated, result.InventorySkipped, result.InventoryFailed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory sync failed for channel {ChannelId}", channelId);
            result.Status = "Failed";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    public async Task<List<ChannelLocationDto>> GetLocationsAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

        if (channel == null)
        {
            throw new InvalidOperationException("Channel not found");
        }

        if (!channel.IsConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var accessToken = channel.AccessToken ?? channel.CredentialsEncrypted;
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Channel credentials are missing");
        }

        var shopDomain = channel.StoreUrl!.Replace("https://", "").Replace("http://", "").TrimEnd('/');

        var locations = await _shopifyClient.GetLocationsAsync(shopDomain, accessToken, cancellationToken);

        _logger.LogInformation("Retrieved {Count} locations from Shopify for channel {ChannelId}",
            locations.Count, channelId);

        return locations.Select(l => new ChannelLocationDto
        {
            Id = l.Id,
            Name = l.Name,
            Address1 = l.Address1,
            Address2 = l.Address2,
            City = l.City,
            Province = l.Province,
            Country = l.Country,
            Zip = l.Zip,
            Phone = l.Phone,
            Active = l.Active,
            Legacy = l.Legacy
        }).ToList();
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
