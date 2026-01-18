using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Service for updating orders on Shopify.
/// </summary>
public class ShopifyOrderUpdateService : IOrderUpdateService
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShopifyClient _shopifyClient;
    private readonly ILogger<ShopifyOrderUpdateService> _logger;

    public ChannelType ChannelType => ChannelType.Shopify;

    public ShopifyOrderUpdateService(
        ITenantDbContext dbContext,
        IShopifyClient shopifyClient,
        ILogger<ShopifyOrderUpdateService> logger)
    {
        _dbContext = dbContext;
        _shopifyClient = shopifyClient;
        _logger = logger;
    }

    public async Task<OrderUpdateResult> UpdateOrderAsync(
        Guid channelId,
        string externalOrderId,
        Order order,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get channel with credentials
            var channel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

            if (channel == null)
            {
                return OrderUpdateResult.Failed("Channel not found");
            }

            if (string.IsNullOrEmpty(channel.AccessToken))
            {
                return OrderUpdateResult.Failed("Channel is not connected. Please reconnect to Shopify.");
            }

            if (string.IsNullOrEmpty(channel.StoreUrl))
            {
                return OrderUpdateResult.Failed("Store URL is not configured for this channel.");
            }

            if (!long.TryParse(externalOrderId, out var shopifyOrderId))
            {
                return OrderUpdateResult.Failed($"Invalid Shopify order ID: {externalOrderId}");
            }

            // Build Shopify order update request
            // Note: Shopify has limitations on what can be updated on an order
            var shopifyUpdateData = BuildShopifyOrderUpdateRequest(order);

            // Update order on Shopify
            var result = await _shopifyClient.UpdateOrderAsync(
                channel.StoreUrl,
                channel.AccessToken,
                shopifyOrderId,
                shopifyUpdateData,
                cancellationToken);

            if (result == null)
            {
                return OrderUpdateResult.Failed("Failed to update order on Shopify. No response received.");
            }

            _logger.LogInformation(
                "Updated order {OrderNumber} on Shopify (ID: {ShopifyOrderId})",
                order.OrderNumber,
                shopifyOrderId);

            return OrderUpdateResult.Succeeded(
                platformData: new Dictionary<string, object>
                {
                    ["shopify_order_id"] = result.Id,
                    ["shopify_updated_at"] = result.UpdatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order {OrderNumber} on Shopify", order.OrderNumber);
            return OrderUpdateResult.Failed($"Failed to update order on Shopify: {ex.Message}");
        }
    }

    private static ShopifyUpdateOrderRequest BuildShopifyOrderUpdateRequest(Order order)
    {
        // Shopify's order update API has limitations:
        // - Cannot change line items after order creation
        // - Can update: note, email, phone, tags, shipping_address, buyer_accepts_marketing
        // - Financial status changes require transactions API
        return new ShopifyUpdateOrderRequest
        {
            Order = new ShopifyUpdateOrderData
            {
                Email = order.CustomerEmail,
                Phone = order.CustomerPhone,
                Note = order.CustomerNotes,
                NoteAttributes = !string.IsNullOrEmpty(order.InternalNotes)
                    ? new List<ShopifyNoteAttribute>
                    {
                        new() { Name = "internal_notes", Value = order.InternalNotes }
                    }
                    : null,
                ShippingAddress = new ShopifyAddressRequest
                {
                    Name = order.ShippingAddress.Name,
                    Address1 = order.ShippingAddress.Line1,
                    Address2 = order.ShippingAddress.Line2,
                    City = order.ShippingAddress.City,
                    Province = order.ShippingAddress.State,
                    Zip = order.ShippingAddress.PostalCode,
                    Country = order.ShippingAddress.Country,
                    Phone = order.ShippingAddress.Phone ?? order.CustomerPhone
                },
                BillingAddress = order.BillingAddress != null
                    ? new ShopifyAddressRequest
                    {
                        Name = order.BillingAddress.Name,
                        Address1 = order.BillingAddress.Line1,
                        Address2 = order.BillingAddress.Line2,
                        City = order.BillingAddress.City,
                        Province = order.BillingAddress.State,
                        Zip = order.BillingAddress.PostalCode,
                        Country = order.BillingAddress.Country,
                        Phone = order.BillingAddress.Phone
                    }
                    : null
            }
        };
    }
}

#region Shopify Update Request Models

public class ShopifyUpdateOrderRequest
{
    public ShopifyUpdateOrderData Order { get; set; } = null!;
}

public class ShopifyUpdateOrderData
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Note { get; set; }
    public List<ShopifyNoteAttribute>? NoteAttributes { get; set; }
    public ShopifyAddressRequest? ShippingAddress { get; set; }
    public ShopifyAddressRequest? BillingAddress { get; set; }
}

#endregion
