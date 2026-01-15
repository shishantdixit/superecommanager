using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Identity;

/// <summary>
/// Represents a user within a tenant.
/// This entity is stored in the tenant-specific schema.
/// </summary>
public class User : AuditableEntity, ISoftDeletable
{
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndsAt { get; private set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { } // EF Core constructor

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone,
            FirstName = firstName.Trim(),
            LastName = lastName?.Trim() ?? string.Empty,
            PasswordHash = passwordHash,
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public void UpdateProfile(string firstName, string lastName, string? phone)
    {
        FirstName = firstName?.Trim() ?? FirstName;
        LastName = lastName?.Trim() ?? string.Empty;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEndsAt = null;
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

    public void AssignRole(Guid roleId, Guid? assignedBy = null)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId, assignedBy));
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
        }
    }
}
