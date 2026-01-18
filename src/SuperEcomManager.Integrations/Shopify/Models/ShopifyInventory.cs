using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Shopify.Models;

/// <summary>
/// Shopify inventory level representation.
/// Represents stock at a specific location for an inventory item.
/// </summary>
public class ShopifyInventoryLevel
{
    [JsonPropertyName("inventory_item_id")]
    public long InventoryItemId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("available")]
    public int? Available { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Shopify inventory item representation.
/// Represents the inventory tracking settings for a variant.
/// </summary>
public class ShopifyInventoryItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("cost")]
    public string? Cost { get; set; }

    [JsonPropertyName("country_code_of_origin")]
    public string? CountryCodeOfOrigin { get; set; }

    [JsonPropertyName("harmonized_system_code")]
    public string? HarmonizedSystemCode { get; set; }

    [JsonPropertyName("tracked")]
    public bool Tracked { get; set; }

    [JsonPropertyName("requires_shipping")]
    public bool RequiresShipping { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Shopify location representation.
/// Represents a physical location where inventory is stocked.
/// </summary>
public class ShopifyLocation
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

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

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("legacy")]
    public bool Legacy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

#region API Response Wrappers

/// <summary>
/// Response wrapper for product list.
/// </summary>
public class ShopifyProductsResponse
{
    [JsonPropertyName("products")]
    public List<ShopifyProduct> Products { get; set; } = new();
}

/// <summary>
/// Response wrapper for single product.
/// </summary>
public class ShopifyProductResponse
{
    [JsonPropertyName("product")]
    public ShopifyProduct? Product { get; set; }
}

/// <summary>
/// Response wrapper for inventory levels list.
/// </summary>
public class ShopifyInventoryLevelsResponse
{
    [JsonPropertyName("inventory_levels")]
    public List<ShopifyInventoryLevel> InventoryLevels { get; set; } = new();
}

/// <summary>
/// Response wrapper for single inventory level.
/// </summary>
public class ShopifyInventoryLevelResponse
{
    [JsonPropertyName("inventory_level")]
    public ShopifyInventoryLevel? InventoryLevel { get; set; }
}

/// <summary>
/// Response wrapper for inventory item.
/// </summary>
public class ShopifyInventoryItemResponse
{
    [JsonPropertyName("inventory_item")]
    public ShopifyInventoryItem? InventoryItem { get; set; }
}

/// <summary>
/// Response wrapper for locations list.
/// </summary>
public class ShopifyLocationsResponse
{
    [JsonPropertyName("locations")]
    public List<ShopifyLocation> Locations { get; set; } = new();
}

/// <summary>
/// Response wrapper for product count.
/// </summary>
public class ShopifyCountResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

#endregion

#region API Request Models

/// <summary>
/// Request to set inventory level.
/// </summary>
public class ShopifySetInventoryLevelRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("inventory_item_id")]
    public long InventoryItemId { get; set; }

    [JsonPropertyName("available")]
    public int Available { get; set; }
}

/// <summary>
/// Request to adjust inventory level.
/// </summary>
public class ShopifyAdjustInventoryLevelRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("inventory_item_id")]
    public long InventoryItemId { get; set; }

    [JsonPropertyName("available_adjustment")]
    public int AvailableAdjustment { get; set; }
}

#endregion
