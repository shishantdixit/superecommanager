using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Integrations.Shopify.Models;
using SuperEcomManager.Integrations.Shopify.Services;

namespace SuperEcomManager.Integrations.Shopify.Webhooks;

/// <summary>
/// Handles incoming webhooks from Shopify.
/// </summary>
public class ShopifyWebhookHandler : IShopifyWebhookHandler
{
    private readonly ShopifyOrderMapper _orderMapper;
    private readonly ILogger<ShopifyWebhookHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Injected per-tenant via property injection
    public DbSet<SalesChannel>? SalesChannels { get; set; }
    public DbSet<Order>? Orders { get; set; }
    public Func<CancellationToken, Task<int>>? SaveChangesAsync { get; set; }

    public ShopifyWebhookHandler(
        ShopifyOrderMapper orderMapper,
        ILogger<ShopifyWebhookHandler> logger)
    {
        _orderMapper = orderMapper;
        _logger = logger;
    }

    public async Task HandleWebhookAsync(
        string topic,
        string shopDomain,
        string payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Shopify webhook: {Topic} from {ShopDomain}", topic, shopDomain);

        if (SalesChannels == null || Orders == null || SaveChangesAsync == null)
        {
            _logger.LogError("WebhookHandler not properly configured with DbContext");
            throw new InvalidOperationException("WebhookHandler not properly configured");
        }

        // Find the channel for this shop
        var channel = await SalesChannels
            .FirstOrDefaultAsync(c => c.StoreUrl == shopDomain && c.IsActive, cancellationToken);

        if (channel == null)
        {
            _logger.LogWarning("No active channel found for shop {ShopDomain}", shopDomain);
            return;
        }

        try
        {
            switch (topic)
            {
                case ShopifyWebhookTopics.OrdersCreate:
                    await HandleOrderCreatedAsync(channel.Id, payload, cancellationToken);
                    break;

                case ShopifyWebhookTopics.OrdersUpdated:
                case ShopifyWebhookTopics.OrdersFulfilled:
                case ShopifyWebhookTopics.OrdersPaid:
                    await HandleOrderUpdatedAsync(channel.Id, payload, cancellationToken);
                    break;

                case ShopifyWebhookTopics.OrdersCancelled:
                    await HandleOrderCancelledAsync(channel.Id, payload, cancellationToken);
                    break;

                case ShopifyWebhookTopics.AppUninstalled:
                    await HandleAppUninstalledAsync(channel, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unhandled webhook topic: {Topic}", topic);
                    break;
            }

            await SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook {Topic} for {ShopDomain}", topic, shopDomain);
            throw;
        }
    }

    private async Task HandleOrderCreatedAsync(
        Guid channelId,
        string payload,
        CancellationToken cancellationToken)
    {
        var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrder>(payload, JsonOptions);
        if (shopifyOrder == null)
        {
            _logger.LogWarning("Failed to deserialize order from webhook payload");
            return;
        }

        var externalOrderId = shopifyOrder.Id.ToString();

        // Check if order already exists (in case of duplicate webhooks)
        var existingOrder = await Orders!
            .AnyAsync(o => o.ChannelId == channelId && o.ExternalOrderId == externalOrderId, cancellationToken);

        if (existingOrder)
        {
            _logger.LogDebug("Order {ExternalOrderId} already exists, skipping creation", externalOrderId);
            return;
        }

        var order = _orderMapper.MapToOrder(shopifyOrder, channelId);
        await Orders.AddAsync(order, cancellationToken);

        _logger.LogInformation("Created order {OrderNumber} from webhook", order.OrderNumber);
    }

    private async Task HandleOrderUpdatedAsync(
        Guid channelId,
        string payload,
        CancellationToken cancellationToken)
    {
        var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrder>(payload, JsonOptions);
        if (shopifyOrder == null)
        {
            _logger.LogWarning("Failed to deserialize order from webhook payload");
            return;
        }

        var externalOrderId = shopifyOrder.Id.ToString();

        var order = await Orders!
            .FirstOrDefaultAsync(o => o.ChannelId == channelId && o.ExternalOrderId == externalOrderId, cancellationToken);

        if (order == null)
        {
            // Order doesn't exist yet, create it
            order = _orderMapper.MapToOrder(shopifyOrder, channelId);
            await Orders.AddAsync(order, cancellationToken);
            _logger.LogInformation("Created missing order {OrderNumber} from update webhook", order.OrderNumber);
        }
        else
        {
            // Update existing order
            _orderMapper.UpdateOrder(order, shopifyOrder);
            _logger.LogInformation("Updated order {OrderNumber} from webhook", order.OrderNumber);
        }
    }

    private async Task HandleOrderCancelledAsync(
        Guid channelId,
        string payload,
        CancellationToken cancellationToken)
    {
        var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrder>(payload, JsonOptions);
        if (shopifyOrder == null)
        {
            _logger.LogWarning("Failed to deserialize order from webhook payload");
            return;
        }

        var externalOrderId = shopifyOrder.Id.ToString();

        var order = await Orders!
            .FirstOrDefaultAsync(o => o.ChannelId == channelId && o.ExternalOrderId == externalOrderId, cancellationToken);

        if (order != null)
        {
            order.Cancel(null, "Cancelled via Shopify");
            _logger.LogInformation("Cancelled order {OrderNumber} from webhook", order.OrderNumber);
        }
        else
        {
            _logger.LogWarning("Order {ExternalOrderId} not found for cancellation webhook", externalOrderId);
        }
    }

    private Task HandleAppUninstalledAsync(
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        channel.Deactivate();
        channel.UpdateCredentials(string.Empty);
        _logger.LogInformation("Deactivated channel {ChannelId} due to app uninstall", channel.Id);
        return Task.CompletedTask;
    }
}
