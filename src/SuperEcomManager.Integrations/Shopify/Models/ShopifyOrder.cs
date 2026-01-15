using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Shopify.Models;

/// <summary>
/// Shopify order representation.
/// </summary>
public class ShopifyOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("order_number")]
    public int OrderNumber { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [JsonPropertyName("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [JsonPropertyName("financial_status")]
    public string? FinancialStatus { get; set; }

    [JsonPropertyName("fulfillment_status")]
    public string? FulfillmentStatus { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";

    [JsonPropertyName("total_price")]
    public string TotalPrice { get; set; } = "0";

    [JsonPropertyName("subtotal_price")]
    public string SubtotalPrice { get; set; } = "0";

    [JsonPropertyName("total_tax")]
    public string TotalTax { get; set; } = "0";

    [JsonPropertyName("total_discounts")]
    public string TotalDiscounts { get; set; } = "0";

    [JsonPropertyName("total_shipping_price_set")]
    public ShopifyPriceSet? TotalShippingPriceSet { get; set; }

    [JsonPropertyName("line_items")]
    public List<ShopifyLineItem> LineItems { get; set; } = new();

    [JsonPropertyName("shipping_address")]
    public ShopifyAddress? ShippingAddress { get; set; }

    [JsonPropertyName("billing_address")]
    public ShopifyAddress? BillingAddress { get; set; }

    [JsonPropertyName("customer")]
    public ShopifyCustomer? Customer { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("discount_codes")]
    public List<ShopifyDiscountCode> DiscountCodes { get; set; } = new();

    [JsonPropertyName("payment_gateway_names")]
    public List<string> PaymentGatewayNames { get; set; } = new();

    [JsonPropertyName("fulfillments")]
    public List<ShopifyFulfillment> Fulfillments { get; set; } = new();

    [JsonPropertyName("refunds")]
    public List<ShopifyRefund> Refunds { get; set; } = new();
}

public class ShopifyLineItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("variant_title")]
    public string? VariantTitle { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";

    [JsonPropertyName("total_discount")]
    public string TotalDiscount { get; set; } = "0";

    [JsonPropertyName("fulfillment_status")]
    public string? FulfillmentStatus { get; set; }

    [JsonPropertyName("requires_shipping")]
    public bool RequiresShipping { get; set; }

    [JsonPropertyName("taxable")]
    public bool Taxable { get; set; }

    [JsonPropertyName("grams")]
    public int Grams { get; set; }

    [JsonPropertyName("properties")]
    public List<ShopifyProperty> Properties { get; set; } = new();
}

public class ShopifyAddress
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("province_code")]
    public string? ProvinceCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }
}

public class ShopifyCustomer
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("orders_count")]
    public int OrdersCount { get; set; }

    [JsonPropertyName("total_spent")]
    public string TotalSpent { get; set; } = "0";

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("default_address")]
    public ShopifyAddress? DefaultAddress { get; set; }
}

public class ShopifyDiscountCode
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ShopifyFulfillment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("tracking_company")]
    public string? TrackingCompany { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_url")]
    public string? TrackingUrl { get; set; }

    [JsonPropertyName("line_items")]
    public List<ShopifyLineItem> LineItems { get; set; } = new();
}

public class ShopifyRefund
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("refund_line_items")]
    public List<ShopifyRefundLineItem> RefundLineItems { get; set; } = new();
}

public class ShopifyRefundLineItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("line_item_id")]
    public long LineItemId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("subtotal")]
    public string Subtotal { get; set; } = "0";
}

public class ShopifyProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class ShopifyPriceSet
{
    [JsonPropertyName("shop_money")]
    public ShopifyMoney? ShopMoney { get; set; }

    [JsonPropertyName("presentment_money")]
    public ShopifyMoney? PresentmentMoney { get; set; }
}

public class ShopifyMoney
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0";

    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "INR";
}
