using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Courier account data transfer object.
/// </summary>
public class CourierAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourierType CourierType { get; set; }
    public string CourierTypeName => CourierType.ToString();
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsConnected { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? LastError { get; set; }
    public int Priority { get; set; }
    public bool SupportsCOD { get; set; }
    public bool SupportsReverse { get; set; }
    public bool SupportsExpress { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Courier account details with settings.
/// </summary>
public class CourierAccountDetailDto : CourierAccountDto
{
    public bool HasApiKey { get; set; }
    public bool HasApiSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public string? AccountId { get; set; }
    public string? ChannelId { get; set; }
    public string? WebhookUrl { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Request to create a courier account.
/// </summary>
public class CreateCourierAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public CourierType CourierType { get; set; }
    public bool IsDefault { get; set; }
    public int Priority { get; set; } = 100;
}

/// <summary>
/// Request to update courier account credentials.
/// </summary>
public class UpdateCourierCredentialsRequest
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountId { get; set; }
    public string? ChannelId { get; set; }
}

/// <summary>
/// Request to update courier account settings.
/// </summary>
public class UpdateCourierSettingsRequest
{
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public bool? SupportsCOD { get; set; }
    public bool? SupportsReverse { get; set; }
    public bool? SupportsExpress { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Available courier type info.
/// </summary>
public class AvailableCourierDto
{
    public CourierType CourierType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAggregator { get; set; }
    public bool RequiresApiKey { get; set; }
    public bool RequiresApiSecret { get; set; }
    public bool RequiresAccessToken { get; set; }
    public List<string> Features { get; set; } = new();
}
