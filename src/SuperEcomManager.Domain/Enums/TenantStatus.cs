namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Tenant account status.
/// </summary>
public enum TenantStatus
{
    /// <summary>Tenant is pending activation/setup</summary>
    Pending = 0,

    /// <summary>Tenant is active and operational</summary>
    Active = 1,

    /// <summary>Tenant is temporarily suspended</summary>
    Suspended = 2,

    /// <summary>Tenant subscription has expired</summary>
    Expired = 3,

    /// <summary>Tenant has been deactivated/cancelled</summary>
    Deactivated = 4
}
