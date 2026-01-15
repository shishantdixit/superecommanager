namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Subscription status.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Trial period active</summary>
    Trial = 0,

    /// <summary>Active paid subscription</summary>
    Active = 1,

    /// <summary>Subscription payment past due</summary>
    PastDue = 2,

    /// <summary>Subscription cancelled but still active until period end</summary>
    Cancelled = 3,

    /// <summary>Subscription expired</summary>
    Expired = 4,

    /// <summary>Subscription paused</summary>
    Paused = 5
}
