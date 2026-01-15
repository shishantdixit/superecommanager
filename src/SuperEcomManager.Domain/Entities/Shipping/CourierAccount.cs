using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Shipping;

/// <summary>
/// Represents a courier account configured for a tenant.
/// Each tenant can have multiple courier accounts (e.g., Shiprocket + Delhivery + BlueDart).
/// </summary>
public class CourierAccount : AuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public CourierType CourierType { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }

    // API Credentials (encrypted)
    public string? ApiKey { get; private set; }
    public string? ApiSecret { get; private set; }
    public string? AccessToken { get; private set; }

    // Account identifiers
    public string? AccountId { get; private set; }
    public string? ChannelId { get; private set; }

    // Webhook configuration
    public string? WebhookUrl { get; private set; }
    public string? WebhookSecret { get; private set; }

    // Settings stored as JSON (warehouse IDs, pickup locations, etc.)
    public string? SettingsJson { get; private set; }

    // Connection status
    public bool IsConnected { get; private set; }
    public DateTime? LastConnectedAt { get; private set; }
    public string? LastError { get; private set; }

    // Rate limiting
    public int? RateLimitPerMinute { get; private set; }

    // Priority for auto-selection (lower = higher priority)
    public int Priority { get; private set; }

    // Service configuration
    public bool SupportsCOD { get; private set; }
    public bool SupportsReverse { get; private set; }
    public bool SupportsExpress { get; private set; }

    // Soft delete
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private CourierAccount() { }

    public static CourierAccount Create(
        string name,
        CourierType courierType,
        bool isDefault = false,
        int priority = 100)
    {
        return new CourierAccount
        {
            Id = Guid.NewGuid(),
            Name = name,
            CourierType = courierType,
            IsActive = false, // Not active until credentials are set
            IsDefault = isDefault,
            IsConnected = false,
            Priority = priority,
            SupportsCOD = true,
            SupportsReverse = false,
            SupportsExpress = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetCredentials(
        string? apiKey,
        string? apiSecret,
        string? accessToken = null,
        string? accountId = null,
        string? channelId = null)
    {
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        AccessToken = accessToken;
        AccountId = accountId;
        ChannelId = channelId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWebhook(string webhookUrl, string? webhookSecret = null)
    {
        WebhookUrl = webhookUrl;
        WebhookSecret = webhookSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(string settingsJson)
    {
        SettingsJson = settingsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetServiceCapabilities(bool supportsCOD, bool supportsReverse, bool supportsExpress)
    {
        SupportsCOD = supportsCOD;
        SupportsReverse = supportsReverse;
        SupportsExpress = supportsExpress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkConnected()
    {
        IsConnected = true;
        IsActive = true;
        LastConnectedAt = DateTime.UtcNow;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDisconnected(string? error = null)
    {
        IsConnected = false;
        LastError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (string.IsNullOrEmpty(ApiKey) && string.IsNullOrEmpty(AccessToken))
            throw new InvalidOperationException("Cannot activate account without credentials");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
