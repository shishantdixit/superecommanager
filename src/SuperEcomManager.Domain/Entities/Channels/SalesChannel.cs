using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Channels;

/// <summary>
/// Represents a connected sales channel (Shopify, Amazon, etc.).
/// </summary>
public class SalesChannel : AuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public ChannelType Type { get; private set; }
    public string? StoreUrl { get; private set; }
    public string? StoreName { get; private set; }
    public string? ExternalShopId { get; private set; }
    public bool IsActive { get; private set; }
    public bool AutoSyncOrders { get; private set; }
    public bool AutoSyncInventory { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncStatus { get; private set; }
    public string? CredentialsEncrypted { get; private set; }
    public string? WebhookSecret { get; private set; }

    // Shopify/OAuth specific fields (stored separately for easy access)
    public string? ApiKey { get; private set; }
    public string? ApiSecret { get; private set; }
    public string? AccessToken { get; private set; }
    public string? Scopes { get; private set; }

    // Connection status
    public bool IsConnected { get; private set; }
    public DateTime? LastConnectedAt { get; private set; }
    public string? LastError { get; private set; }

    // Sync settings
    public int? InitialSyncDays { get; private set; } = 7; // null = all orders, 7 = last 7 days, etc.
    public bool SyncProductsEnabled { get; private set; }
    public bool AutoSyncProducts { get; private set; }
    public DateTime? LastProductSyncAt { get; private set; }
    public DateTime? LastInventorySyncAt { get; private set; }

    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private SalesChannel() { }

    public static SalesChannel Create(
        string name,
        ChannelType type,
        string? storeUrl = null,
        string? storeName = null)
    {
        return new SalesChannel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            StoreUrl = storeUrl,
            StoreName = storeName ?? name,
            IsActive = true,
            AutoSyncOrders = true,
            AutoSyncInventory = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateCredentials(string encryptedCredentials)
    {
        CredentialsEncrypted = encryptedCredentials;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStoreName(string storeName)
    {
        StoreName = storeName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExternalId(string externalShopId)
    {
        ExternalShopId = externalShopId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSync(bool success, string? status = null)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = success ? "Success" : $"Failed: {status}";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public void UpdateSyncSettings(bool autoSyncOrders, bool autoSyncInventory)
    {
        AutoSyncOrders = autoSyncOrders;
        AutoSyncInventory = autoSyncInventory;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAdvancedSyncSettings(
        int? initialSyncDays,
        bool syncProductsEnabled,
        bool autoSyncProducts)
    {
        InitialSyncDays = initialSyncDays;
        SyncProductsEnabled = syncProductsEnabled;
        AutoSyncProducts = autoSyncProducts;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordProductSync()
    {
        LastProductSyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordInventorySync()
    {
        LastInventorySyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApiCredentials(string apiKey, string apiSecret, string? scopes = null)
    {
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        Scopes = scopes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAccessToken(string accessToken)
    {
        AccessToken = accessToken;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkConnected()
    {
        IsConnected = true;
        LastConnectedAt = DateTime.UtcNow;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDisconnected(string? error = null)
    {
        IsConnected = false;
        AccessToken = null;
        LastError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCredentials()
    {
        ApiKey = null;
        ApiSecret = null;
        AccessToken = null;
        Scopes = null;
        IsConnected = false;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
