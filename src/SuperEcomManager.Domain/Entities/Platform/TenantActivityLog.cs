using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Platform;

/// <summary>
/// Tracks administrative actions performed on tenants by platform admins.
/// Stored in shared schema for audit purposes.
/// </summary>
public class TenantActivityLog : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid PerformedBy { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime PerformedAt { get; private set; }

    private TenantActivityLog() { }

    public static TenantActivityLog Create(
        Guid tenantId,
        Guid performedBy,
        string action,
        string? details = null,
        string? ipAddress = null)
    {
        return new TenantActivityLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PerformedBy = performedBy,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            PerformedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Standard tenant activity actions.
/// </summary>
public static class TenantActivityActions
{
    public const string Created = "TENANT_CREATED";
    public const string Activated = "TENANT_ACTIVATED";
    public const string Suspended = "TENANT_SUSPENDED";
    public const string Reactivated = "TENANT_REACTIVATED";
    public const string Deactivated = "TENANT_DEACTIVATED";
    public const string TrialExtended = "TRIAL_EXTENDED";
    public const string PlanChanged = "PLAN_CHANGED";
    public const string ProfileUpdated = "PROFILE_UPDATED";
    public const string OwnerChanged = "OWNER_CHANGED";
    public const string DataExported = "DATA_EXPORTED";
    public const string DataDeleted = "DATA_DELETED";
    public const string SubscriptionActivated = "SUBSCRIPTION_ACTIVATED";
    public const string SubscriptionCancelled = "SUBSCRIPTION_CANCELLED";
    public const string SubscriptionRenewed = "SUBSCRIPTION_RENEWED";
    public const string SubscriptionPaused = "SUBSCRIPTION_PAUSED";
    public const string SubscriptionResumed = "SUBSCRIPTION_RESUMED";
}
