namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Notification channel types.
/// </summary>
public enum NotificationType
{
    /// <summary>Email notification</summary>
    Email = 1,

    /// <summary>SMS notification</summary>
    SMS = 2,

    /// <summary>WhatsApp notification</summary>
    WhatsApp = 3,

    /// <summary>In-app push notification</summary>
    Push = 4,

    /// <summary>In-app notification</summary>
    InApp = 5
}
