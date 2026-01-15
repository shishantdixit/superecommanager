using System.Text.Json;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;
using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify.Services;

/// <summary>
/// Maps Shopify orders to internal Order entities.
/// </summary>
public class ShopifyOrderMapper
{
    /// <summary>
    /// Maps a Shopify order to an internal Order entity.
    /// </summary>
    public Order MapToOrder(ShopifyOrder shopifyOrder, Guid channelId)
    {
        var customerName = GetCustomerName(shopifyOrder);
        var shippingAddress = MapAddress(shopifyOrder.ShippingAddress, customerName);
        var totalAmount = new Money(ParseDecimal(shopifyOrder.TotalPrice), shopifyOrder.Currency ?? "INR");

        // Create the order with required fields
        var order = Order.Create(
            channelId: channelId,
            externalOrderId: shopifyOrder.Id.ToString(),
            customerName: customerName,
            shippingAddress: shippingAddress,
            totalAmount: totalAmount,
            orderDate: shopifyOrder.CreatedAt,
            externalOrderNumber: shopifyOrder.Name
        );

        // Set additional customer info
        var customerEmail = shopifyOrder.Email ?? shopifyOrder.Customer?.Email;
        var customerPhone = shopifyOrder.Phone ?? shopifyOrder.ShippingAddress?.Phone ?? shopifyOrder.Customer?.Phone;
        order.SetCustomerInfo(customerEmail, customerPhone);

        // Set billing address if different
        if (shopifyOrder.BillingAddress != null)
        {
            var billingAddress = MapAddress(shopifyOrder.BillingAddress, customerName);
            order.SetBillingAddress(billingAddress);
        }

        // Set payment info
        var paymentStatus = MapPaymentStatus(shopifyOrder.FinancialStatus);
        var paymentMethod = DeterminePaymentMethod(shopifyOrder);
        order.SetPaymentInfo(paymentMethod, paymentStatus);

        // Set financial breakdown
        var currency = shopifyOrder.Currency ?? "INR";
        var subtotal = new Money(ParseDecimal(shopifyOrder.SubtotalPrice), currency);
        var discountAmount = new Money(ParseDecimal(shopifyOrder.TotalDiscounts), currency);
        var taxAmount = new Money(ParseDecimal(shopifyOrder.TotalTax), currency);
        var shippingAmount = new Money(GetShippingCost(shopifyOrder), currency);
        order.SetFinancials(subtotal, discountAmount, taxAmount, shippingAmount);

        // Set notes
        order.SetNotes(shopifyOrder.Note, null);

        // Store original Shopify data for reference
        order.SetPlatformData(JsonSerializer.Serialize(new
        {
            shopifyOrder.Id,
            shopifyOrder.Name,
            shopifyOrder.Tags,
            shopifyOrder.FulfillmentStatus,
            shopifyOrder.FinancialStatus,
            shopifyOrder.PaymentGatewayNames
        }));

        // Update status based on Shopify status
        var status = MapOrderStatus(shopifyOrder);
        if (status != OrderStatus.Pending)
        {
            order.UpdateStatus(status, null, "Imported from Shopify");
        }

        // Add line items
        foreach (var lineItem in shopifyOrder.LineItems)
        {
            var orderItem = MapLineItem(order.Id, lineItem, currency);
            order.AddItem(orderItem);
        }

        return order;
    }

    /// <summary>
    /// Updates an existing order from Shopify data.
    /// </summary>
    public void UpdateOrder(Order order, ShopifyOrder shopifyOrder)
    {
        var newStatus = MapOrderStatus(shopifyOrder);
        if (order.Status != newStatus)
        {
            order.UpdateStatus(newStatus, null, "Updated from Shopify sync");
        }

        var newPaymentStatus = MapPaymentStatus(shopifyOrder.FinancialStatus);
        if (order.PaymentStatus != newPaymentStatus)
        {
            order.UpdatePaymentStatus(newPaymentStatus);
        }
    }

    private static Address MapAddress(ShopifyAddress? shopifyAddress, string fallbackName)
    {
        if (shopifyAddress == null)
        {
            // Return a minimal valid address
            return new Address(
                name: fallbackName,
                phone: null,
                line1: "Unknown",
                line2: null,
                city: "Unknown",
                state: "Unknown",
                postalCode: "000000",
                country: "India"
            );
        }

        var name = shopifyAddress.Name ??
                  $"{shopifyAddress.FirstName} {shopifyAddress.LastName}".Trim();
        if (string.IsNullOrEmpty(name))
            name = fallbackName;

        var line2Parts = new List<string>();
        if (!string.IsNullOrEmpty(shopifyAddress.Address2))
            line2Parts.Add(shopifyAddress.Address2);
        if (!string.IsNullOrEmpty(shopifyAddress.Company))
            line2Parts.Add(shopifyAddress.Company);

        return new Address(
            name: name,
            phone: shopifyAddress.Phone,
            line1: !string.IsNullOrWhiteSpace(shopifyAddress.Address1) ? shopifyAddress.Address1 : "Unknown",
            line2: line2Parts.Count > 0 ? string.Join(", ", line2Parts) : null,
            city: !string.IsNullOrWhiteSpace(shopifyAddress.City) ? shopifyAddress.City : "Unknown",
            state: !string.IsNullOrWhiteSpace(shopifyAddress.Province) ? shopifyAddress.Province :
                   !string.IsNullOrWhiteSpace(shopifyAddress.ProvinceCode) ? shopifyAddress.ProvinceCode : "Unknown",
            postalCode: !string.IsNullOrWhiteSpace(shopifyAddress.Zip) ? shopifyAddress.Zip : "000000",
            country: shopifyAddress.Country ?? "India"
        );
    }

    private static OrderItem MapLineItem(Guid orderId, ShopifyLineItem lineItem, string currency)
    {
        var unitPrice = new Money(ParseDecimal(lineItem.Price), currency);

        var item = new OrderItem(
            orderId: orderId,
            sku: !string.IsNullOrEmpty(lineItem.Sku) ? lineItem.Sku : $"SHOPIFY-{lineItem.ProductId}",
            name: lineItem.Title,
            quantity: lineItem.Quantity,
            unitPrice: unitPrice,
            externalProductId: lineItem.ProductId?.ToString(),
            variantName: lineItem.VariantTitle
        );

        // Set discount if any
        var discount = new Money(ParseDecimal(lineItem.TotalDiscount), currency);
        if (discount.Amount > 0)
        {
            item.SetFinancials(discount, Money.Zero);
        }

        return item;
    }

    private static string GetCustomerName(ShopifyOrder order)
    {
        if (order.Customer != null)
        {
            var name = $"{order.Customer.FirstName} {order.Customer.LastName}".Trim();
            if (!string.IsNullOrEmpty(name))
                return name;
        }

        if (order.ShippingAddress != null)
        {
            var name = order.ShippingAddress.Name ??
                      $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}".Trim();
            if (!string.IsNullOrEmpty(name))
                return name;
        }

        return "Unknown Customer";
    }

    private static decimal GetShippingCost(ShopifyOrder order)
    {
        return ParseDecimal(order.TotalShippingPriceSet?.ShopMoney?.Amount ?? "0");
    }

    private static PaymentMethod DeterminePaymentMethod(ShopifyOrder order)
    {
        var gateways = order.PaymentGatewayNames;
        if (gateways == null || gateways.Count == 0)
            return PaymentMethod.Other;

        foreach (var gateway in gateways)
        {
            var g = gateway.ToLowerInvariant();

            if (g.Contains("cod") || g.Contains("cash on delivery"))
                return PaymentMethod.COD;

            if (g.Contains("upi") || g.Contains("razorpay") || g.Contains("phonepe") || g.Contains("gpay"))
                return PaymentMethod.UPI;

            if (g.Contains("card") || g.Contains("stripe") || g.Contains("visa") || g.Contains("mastercard"))
                return PaymentMethod.Card;

            if (g.Contains("netbanking") || g.Contains("net banking"))
                return PaymentMethod.NetBanking;

            if (g.Contains("wallet") || g.Contains("paytm"))
                return PaymentMethod.Wallet;

            if (g.Contains("emi") || g.Contains("bnpl"))
                return PaymentMethod.EMI;
        }

        // Default to Other for unknown prepaid methods
        return PaymentMethod.Other;
    }

    private static OrderStatus MapOrderStatus(ShopifyOrder order)
    {
        if (order.CancelledAt.HasValue)
            return OrderStatus.Cancelled;

        if (order.ClosedAt.HasValue)
            return OrderStatus.Delivered;

        return order.FulfillmentStatus?.ToLowerInvariant() switch
        {
            "fulfilled" => OrderStatus.Shipped,
            "partial" => OrderStatus.Processing,
            null or "" => order.FinancialStatus?.ToLowerInvariant() == "paid"
                ? OrderStatus.Confirmed
                : OrderStatus.Pending,
            _ => OrderStatus.Processing
        };
    }

    private static PaymentStatus MapPaymentStatus(string? financialStatus)
    {
        return financialStatus?.ToLowerInvariant() switch
        {
            "paid" => PaymentStatus.Paid,
            "partially_paid" => PaymentStatus.PartiallyPaid,
            "pending" => PaymentStatus.Pending,
            "refunded" => PaymentStatus.Refunded,
            "partially_refunded" => PaymentStatus.PartiallyRefunded,
            "voided" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return decimal.TryParse(value, out var result) ? result : 0;
    }
}
