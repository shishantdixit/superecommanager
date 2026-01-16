using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Platform;

/// <summary>
/// Represents a platform administrator who can manage tenants and platform settings.
/// Stored in shared schema - separate from tenant users.
/// </summary>
public class PlatformAdmin : AuditableEntity, ISoftDeletable
{
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsSuperAdmin { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndsAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public string? LastLoginIpAddress { get; private set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private PlatformAdmin() { }

    public static PlatformAdmin Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        bool isSuperAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new PlatformAdmin
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName?.Trim() ?? string.Empty,
            LastName = lastName?.Trim() ?? string.Empty,
            PasswordHash = passwordHash,
            IsActive = true,
            IsSuperAdmin = isSuperAdmin,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName?.Trim() ?? FirstName;
        LastName = lastName?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin(string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIpAddress = ipAddress;
        FailedLoginAttempts = 0;
        LockoutEndsAt = null;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public void RecordFailedLogin(int maxAttempts = 5, int lockoutMinutes = 30)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndsAt = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    public bool IsLockedOut()
    {
        return LockoutEndsAt.HasValue && LockoutEndsAt.Value > DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PromoteToSuperAdmin()
    {
        IsSuperAdmin = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DemoteFromSuperAdmin()
    {
        IsSuperAdmin = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
