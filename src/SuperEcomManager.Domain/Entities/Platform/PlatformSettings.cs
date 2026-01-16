using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Platform;

/// <summary>
/// Platform-wide settings and configuration.
/// Stored in shared schema.
/// </summary>
public class PlatformSettings : AuditableEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public bool IsEncrypted { get; private set; }

    private PlatformSettings() { }

    public static PlatformSettings Create(
        string key,
        string value,
        string category,
        string? description = null,
        bool isPublic = false,
        bool isEncrypted = false)
    {
        return new PlatformSettings
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            Category = category,
            Description = description,
            IsPublic = isPublic,
            IsEncrypted = isEncrypted,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(string? description, bool isPublic)
    {
        Description = description;
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Standard setting categories.
/// </summary>
public static class SettingCategories
{
    public const string General = "general";
    public const string Email = "email";
    public const string Security = "security";
    public const string Payment = "payment";
    public const string Notification = "notification";
    public const string Integration = "integration";
    public const string Feature = "feature";
}

/// <summary>
/// Standard setting keys.
/// </summary>
public static class SettingKeys
{
    // General
    public const string PlatformName = "platform.name";
    public const string PlatformLogo = "platform.logo_url";
    public const string SupportEmail = "platform.support_email";
    public const string SupportPhone = "platform.support_phone";
    public const string TermsUrl = "platform.terms_url";
    public const string PrivacyUrl = "platform.privacy_url";

    // Security
    public const string PasswordMinLength = "security.password_min_length";
    public const string SessionTimeoutMinutes = "security.session_timeout_minutes";
    public const string MaxLoginAttempts = "security.max_login_attempts";
    public const string LockoutDurationMinutes = "security.lockout_duration_minutes";
    public const string RequireTwoFactor = "security.require_two_factor";

    // Email
    public const string SmtpHost = "email.smtp_host";
    public const string SmtpPort = "email.smtp_port";
    public const string SmtpUsername = "email.smtp_username";
    public const string SmtpPassword = "email.smtp_password";
    public const string SmtpFromEmail = "email.from_email";
    public const string SmtpFromName = "email.from_name";

    // Features
    public const string MaintenanceMode = "feature.maintenance_mode";
    public const string RegistrationEnabled = "feature.registration_enabled";
    public const string TrialDays = "feature.trial_days";
    public const string MaxTenantsPerUser = "feature.max_tenants_per_user";
}
