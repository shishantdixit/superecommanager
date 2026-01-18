using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Sales channel data transfer object.
/// </summary>
public class ChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public bool IsActive { get; set; }
    public string? StoreUrl { get; set; }
    public string? StoreName { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int TotalOrders { get; set; }
    public ChannelSyncStatus SyncStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool AutoSyncOrders { get; set; }
    public bool AutoSyncInventory { get; set; }

    // Credential status (for OAuth channels like Shopify)
    public bool IsConnected { get; set; }
    public bool HasCredentials { get; set; }
    public string? LastError { get; set; }

    // Advanced sync settings
    public int? InitialSyncDays { get; set; }
    public int? InventorySyncDays { get; set; }
    public int? ProductSyncDays { get; set; }
    public int? OrderSyncLimit { get; set; }
    public int? InventorySyncLimit { get; set; }
    public int? ProductSyncLimit { get; set; }
    public bool SyncProductsEnabled { get; set; }
    public bool AutoSyncProducts { get; set; }
    public DateTime? LastProductSyncAt { get; set; }
    public DateTime? LastInventorySyncAt { get; set; }
}

/// <summary>
/// Request to update channel settings.
/// </summary>
public class UpdateChannelSettingsRequest
{
    public bool? AutoSyncOrders { get; set; }
    public bool? AutoSyncInventory { get; set; }

    // Advanced sync settings
    public int? InitialSyncDays { get; set; }
    public int? InventorySyncDays { get; set; }
    public int? ProductSyncDays { get; set; }
    public int? OrderSyncLimit { get; set; }
    public int? InventorySyncLimit { get; set; }
    public int? ProductSyncLimit { get; set; }
    public bool? SyncProductsEnabled { get; set; }
    public bool? AutoSyncProducts { get; set; }
}

/// <summary>
/// Shopify connection request (for OAuth initiation after credentials are saved).
/// </summary>
public class ConnectShopifyRequest
{
    public string ShopDomain { get; set; } = string.Empty;
}

/// <summary>
/// Request to save Shopify API credentials.
/// Each tenant must create their own Shopify app and provide these credentials.
/// </summary>
public class SaveShopifyCredentialsRequest
{
    public Guid? ChannelId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string ShopDomain { get; set; } = string.Empty;
    public string? Scopes { get; set; }
}

/// <summary>
/// OAuth callback request.
/// </summary>
public class ShopifyOAuthCallback
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Shop { get; set; } = string.Empty;
}

/// <summary>
/// Request to save Shopify access token directly for Custom Apps.
/// Custom Apps created in Shopify Admin provide access tokens directly without OAuth.
/// </summary>
public class SaveAccessTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Channel sync status enumeration.
/// </summary>
public enum ChannelSyncStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}
