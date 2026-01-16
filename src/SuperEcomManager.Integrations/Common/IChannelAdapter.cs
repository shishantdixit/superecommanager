using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Common;

/// <summary>
/// Base interface for all sales channel adapters.
/// Implements adapter pattern for multi-channel integration.
/// </summary>
public interface IChannelAdapter
{
    /// <summary>
    /// The channel type this adapter handles.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Display name of the channel.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Validates the connection credentials.
    /// </summary>
    Task<ChannelConnectionResult> ValidateConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs orders from the channel.
    /// </summary>
    Task<ChannelSyncResult> SyncOrdersAsync(
        Guid channelId,
        ChannelCredentials credentials,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs inventory to the channel.
    /// </summary>
    Task<ChannelSyncResult> SyncInventoryAsync(
        Guid channelId,
        ChannelCredentials credentials,
        IEnumerable<InventorySyncItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates shipment tracking information on the channel.
    /// </summary>
    Task<ChannelOperationResult> UpdateShipmentAsync(
        ChannelCredentials credentials,
        ShipmentUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an order on the channel.
    /// </summary>
    Task<ChannelOperationResult> CancelOrderAsync(
        ChannelCredentials credentials,
        string externalOrderId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order details from the channel.
    /// </summary>
    Task<ChannelOrder?> GetOrderAsync(
        ChannelCredentials credentials,
        string externalOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the OAuth URL for channel connection (if applicable).
    /// </summary>
    Task<string?> GetOAuthUrlAsync(
        string redirectUri,
        string state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes OAuth flow and returns credentials.
    /// </summary>
    Task<ChannelCredentials?> CompleteOAuthAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this channel supports OAuth authentication.
    /// </summary>
    bool SupportsOAuth { get; }

    /// <summary>
    /// Whether this channel supports inventory sync.
    /// </summary>
    bool SupportsInventorySync { get; }

    /// <summary>
    /// Whether this channel supports order cancellation from external system.
    /// </summary>
    bool SupportsOrderCancellation { get; }
}

/// <summary>
/// Channel connection credentials.
/// </summary>
public class ChannelCredentials
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? ShopDomain { get; set; }
    public string? SellerId { get; set; }
    public string? MarketplaceId { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();

    /// <summary>
    /// Creates credentials from encrypted JSON.
    /// </summary>
    public static ChannelCredentials FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new ChannelCredentials();

        return System.Text.Json.JsonSerializer.Deserialize<ChannelCredentials>(json)
            ?? new ChannelCredentials();
    }

    /// <summary>
    /// Serializes credentials to JSON for storage.
    /// </summary>
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}

/// <summary>
/// Result of a channel connection validation.
/// </summary>
public class ChannelConnectionResult
{
    public bool IsConnected { get; set; }
    public string? ShopName { get; set; }
    public string? ShopId { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static ChannelConnectionResult Success(string shopName, string? shopId = null)
    {
        return new ChannelConnectionResult
        {
            IsConnected = true,
            ShopName = shopName,
            ShopId = shopId
        };
    }

    public static ChannelConnectionResult Failed(string error)
    {
        return new ChannelConnectionResult
        {
            IsConnected = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class ChannelSyncResult
{
    public bool Success { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsFailed { get; set; }
    public int ItemsSkipped { get; set; }
    public string Status { get; set; } = "Completed";
    public List<string> Errors { get; set; } = new();
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    public static ChannelSyncResult Completed(int created, int updated, int skipped = 0)
    {
        return new ChannelSyncResult
        {
            Success = true,
            ItemsProcessed = created + updated + skipped,
            ItemsCreated = created,
            ItemsUpdated = updated,
            ItemsSkipped = skipped,
            Status = "Completed"
        };
    }

    public static ChannelSyncResult Failed(string error)
    {
        return new ChannelSyncResult
        {
            Success = false,
            Status = "Failed",
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Result of a channel operation.
/// </summary>
public class ChannelOperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public static ChannelOperationResult Succeeded(string? externalId = null)
    {
        return new ChannelOperationResult
        {
            Success = true,
            ExternalId = externalId
        };
    }

    public static ChannelOperationResult Failed(string error)
    {
        return new ChannelOperationResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Inventory item for sync operations.
/// </summary>
public class InventorySyncItem
{
    public string Sku { get; set; } = string.Empty;
    public string? ExternalProductId { get; set; }
    public string? ExternalVariantId { get; set; }
    public int Quantity { get; set; }
    public string? LocationId { get; set; }
}

/// <summary>
/// Request to update shipment on channel.
/// </summary>
public class ShipmentUpdateRequest
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    public string? CarrierName { get; set; }
    public string? CarrierCode { get; set; }
    public DateTime? ShippedAt { get; set; }
    public bool NotifyCustomer { get; set; } = true;
    public List<ShipmentLineItem> LineItems { get; set; } = new();
}

/// <summary>
/// Line item in a shipment update.
/// </summary>
public class ShipmentLineItem
{
    public string ExternalLineItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// Unified order model from any channel.
/// </summary>
public class ChannelOrder
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public string? ExternalOrderNumber { get; set; }
    public ChannelType ChannelType { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinancialStatus { get; set; }
    public string? FulfillmentStatus { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCOD { get; set; }
    public decimal? CodAmount { get; set; }
    public ChannelCustomer Customer { get; set; } = new();
    public ChannelAddress ShippingAddress { get; set; } = new();
    public ChannelAddress? BillingAddress { get; set; }
    public List<ChannelOrderItem> Items { get; set; } = new();
    public string? Note { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Customer information from channel.
/// </summary>
public class ChannelCustomer
{
    public string? ExternalCustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>
/// Address information from channel.
/// </summary>
public class ChannelAddress
{
    public string? Name { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "IN";
    public string? Phone { get; set; }
}

/// <summary>
/// Order line item from channel.
/// </summary>
public class ChannelOrderItem
{
    public string ExternalLineItemId { get; set; } = string.Empty;
    public string? ExternalProductId { get; set; }
    public string? ExternalVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public int? Weight { get; set; }
    public string? ImageUrl { get; set; }
}
