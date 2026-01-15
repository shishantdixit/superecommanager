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
}

/// <summary>
/// Shopify connection request.
/// </summary>
public class ConnectShopifyRequest
{
    public string ShopDomain { get; set; } = string.Empty;
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
/// Channel sync status enumeration.
/// </summary>
public enum ChannelSyncStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}
