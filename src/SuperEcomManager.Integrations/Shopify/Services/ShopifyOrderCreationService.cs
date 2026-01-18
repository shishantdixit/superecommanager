using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Service for creating orders on Shopify.
/// </summary>
public class ShopifyOrderCreationService : IOrderCreationService
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShopifyClient _shopifyClient;
    private readonly ILogger<ShopifyOrderCreationService> _logger;

    public ChannelType ChannelType => ChannelType.Shopify;

    public ShopifyOrderCreationService(
        ITenantDbContext dbContext,
        IShopifyClient shopifyClient,
        ILogger<ShopifyOrderCreationService> logger)
    {
        _dbContext = dbContext;
        _shopifyClient = shopifyClient;
        _logger = logger;
    }

    public async Task<OrderCreationResult> CreateOrderAsync(
        Guid channelId,
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
                return OrderCreationResult.Failed("Channel not found");
            }

            if (string.IsNullOrEmpty(channel.AccessToken))
            {
                return OrderCreationResult.Failed("Channel is not connected. Please reconnect to Shopify.");
            }

            if (string.IsNullOrEmpty(channel.StoreUrl))
            {
                return OrderCreationResult.Failed("Store URL is not configured for this channel.");
            }

            // Build Shopify order request
            var shopifyOrder = BuildShopifyOrderRequest(order);

            // Create order on Shopify
            var result = await _shopifyClient.CreateOrderAsync(
                channel.StoreUrl,
                channel.AccessToken,
                shopifyOrder,
                cancellationToken);

            if (result == null)
            {
                return OrderCreationResult.Failed("Failed to create order on Shopify. No response received.");
            }

            _logger.LogInformation(
                "Created order {OrderNumber} on Shopify as {ShopifyOrderId}",
                order.OrderNumber,
                result.Id);

            return OrderCreationResult.Succeeded(
                externalOrderId: result.Id.ToString(),
                externalOrderNumber: result.OrderNumber > 0 ? result.OrderNumber.ToString() : result.Name,
                platformData: new Dictionary<string, object>
                {
                    ["shopify_order_id"] = result.Id,
                    ["shopify_order_number"] = result.OrderNumber,
                    ["shopify_order_name"] = result.Name ?? "",
                    ["shopify_created_at"] = result.CreatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order {OrderNumber} on Shopify", order.OrderNumber);
            return OrderCreationResult.Failed($"Failed to create order on Shopify: {ex.Message}");
        }
    }

    private static ShopifyCreateOrderRequest BuildShopifyOrderRequest(Order order)
    {
        return new ShopifyCreateOrderRequest
        {
            Order = new ShopifyCreateOrderData
            {
                Email = order.CustomerEmail,
                Phone = order.CustomerPhone,
                FinancialStatus = MapPaymentStatus(order.PaymentStatus),
                FulfillmentStatus = null, // Will be null for new orders
                Currency = order.TotalAmount.Currency,
                Note = order.CustomerNotes,
                NoteAttributes = !string.IsNullOrEmpty(order.InternalNotes)
                    ? new List<ShopifyNoteAttribute>
                    {
                        new() { Name = "internal_notes", Value = order.InternalNotes }
                    }
                    : null,
                LineItems = order.Items.Select(item => new ShopifyLineItemRequest
                {
                    Title = item.Name,
                    Sku = item.Sku,
                    VariantTitle = item.VariantName,
                    Quantity = item.Quantity,
                    Price = item.UnitPrice.Amount.ToString("F2"),
                    RequiresShipping = true,
                    Taxable = item.TaxAmount.Amount > 0
                }).ToList(),
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
                    : null,
                Customer = new ShopifyCustomerRequest
                {
                    Email = order.CustomerEmail,
                    Phone = order.CustomerPhone,
                    FirstName = GetFirstName(order.CustomerName),
                    LastName = GetLastName(order.CustomerName)
                },
                ShippingLines = order.ShippingAmount.Amount > 0
                    ? new List<ShopifyShippingLineRequest>
                    {
                        new()
                        {
                            Title = "Shipping",
                            Price = order.ShippingAmount.Amount.ToString("F2"),
                            Code = "SHIPPING"
                        }
                    }
                    : null,
                TotalDiscounts = order.DiscountAmount.Amount.ToString("F2"),
                TaxLines = order.TaxAmount.Amount > 0
                    ? new List<ShopifyTaxLineRequest>
                    {
                        new()
                        {
                            Title = "Tax",
                            Price = order.TaxAmount.Amount.ToString("F2"),
                            Rate = 0.18m // Default GST rate
                        }
                    }
                    : null,
                Tags = "manual_order,superecommanager",
                SendReceipt = false,
                SendFulfillmentReceipt = false
            }
        };
    }

    private static string MapPaymentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "paid",
        PaymentStatus.PartiallyPaid => "partially_paid",
        PaymentStatus.Pending => "pending",
        PaymentStatus.Refunded => "refunded",
        PaymentStatus.PartiallyRefunded => "partially_refunded",
        _ => "pending"
    };

    private static string GetFirstName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }

    private static string GetLastName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
    }
}

#region Shopify Request Models

public class ShopifyCreateOrderRequest
{
    public ShopifyCreateOrderData Order { get; set; } = null!;
}

public class ShopifyCreateOrderData
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FinancialStatus { get; set; }
    public string? FulfillmentStatus { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
    public List<ShopifyNoteAttribute>? NoteAttributes { get; set; }
    public List<ShopifyLineItemRequest> LineItems { get; set; } = new();
    public ShopifyAddressRequest? ShippingAddress { get; set; }
    public ShopifyAddressRequest? BillingAddress { get; set; }
    public ShopifyCustomerRequest? Customer { get; set; }
    public List<ShopifyShippingLineRequest>? ShippingLines { get; set; }
    public string? TotalDiscounts { get; set; }
    public List<ShopifyTaxLineRequest>? TaxLines { get; set; }
    public string? Tags { get; set; }
    public bool SendReceipt { get; set; }
    public bool SendFulfillmentReceipt { get; set; }
}

public class ShopifyNoteAttribute
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ShopifyLineItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? VariantTitle { get; set; }
    public int Quantity { get; set; }
    public string Price { get; set; } = "0.00";
    public bool RequiresShipping { get; set; }
    public bool Taxable { get; set; }
}

public class ShopifyAddressRequest
{
    public string? Name { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Zip { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
}

public class ShopifyCustomerRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class ShopifyShippingLineRequest
{
    public string Title { get; set; } = string.Empty;
    public string Price { get; set; } = "0.00";
    public string? Code { get; set; }
}

public class ShopifyTaxLineRequest
{
    public string Title { get; set; } = string.Empty;
    public string Price { get; set; } = "0.00";
    public decimal Rate { get; set; }
}

#endregion
