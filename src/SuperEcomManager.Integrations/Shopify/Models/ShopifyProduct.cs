using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Shopify.Models;

/// <summary>
/// Shopify product representation.
/// </summary>
public class ShopifyProduct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body_html")]
    public string? BodyHtml { get; set; }

    [JsonPropertyName("vendor")]
    public string? Vendor { get; set; }

    [JsonPropertyName("product_type")]
    public string? ProductType { get; set; }

    [JsonPropertyName("handle")]
    public string Handle { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("variants")]
    public List<ShopifyVariant> Variants { get; set; } = new();

    [JsonPropertyName("options")]
    public List<ShopifyProductOption> Options { get; set; } = new();

    [JsonPropertyName("images")]
    public List<ShopifyProductImage> Images { get; set; } = new();

    [JsonPropertyName("image")]
    public ShopifyProductImage? Image { get; set; }
}

/// <summary>
/// Shopify product variant representation.
/// </summary>
public class ShopifyVariant
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0.00";

    [JsonPropertyName("compare_at_price")]
    public string? CompareAtPrice { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("option1")]
    public string? Option1 { get; set; }

    [JsonPropertyName("option2")]
    public string? Option2 { get; set; }

    [JsonPropertyName("option3")]
    public string? Option3 { get; set; }

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("weight_unit")]
    public string WeightUnit { get; set; } = "kg";

    [JsonPropertyName("inventory_item_id")]
    public long InventoryItemId { get; set; }

    [JsonPropertyName("inventory_quantity")]
    public int InventoryQuantity { get; set; }

    [JsonPropertyName("inventory_management")]
    public string? InventoryManagement { get; set; }

    [JsonPropertyName("inventory_policy")]
    public string InventoryPolicy { get; set; } = "deny";

    [JsonPropertyName("fulfillment_service")]
    public string FulfillmentService { get; set; } = "manual";

    [JsonPropertyName("requires_shipping")]
    public bool RequiresShipping { get; set; } = true;

    [JsonPropertyName("taxable")]
    public bool Taxable { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("image_id")]
    public long? ImageId { get; set; }
}

/// <summary>
/// Shopify product option representation.
/// </summary>
public class ShopifyProductOption
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new();
}

/// <summary>
/// Shopify product image representation.
/// </summary>
public class ShopifyProductImage
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("variant_ids")]
    public List<long> VariantIds { get; set; } = new();
}
