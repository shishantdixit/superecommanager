namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Sales channel types supported by the platform.
/// </summary>
public enum ChannelType
{
    /// <summary>Shopify store</summary>
    Shopify = 1,

    /// <summary>Amazon marketplace</summary>
    Amazon = 2,

    /// <summary>Flipkart marketplace</summary>
    Flipkart = 3,

    /// <summary>Meesho marketplace</summary>
    Meesho = 4,

    /// <summary>WooCommerce store</summary>
    WooCommerce = 5,

    /// <summary>Custom/Direct website integration</summary>
    Custom = 99
}
