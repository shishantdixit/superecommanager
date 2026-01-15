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
}
