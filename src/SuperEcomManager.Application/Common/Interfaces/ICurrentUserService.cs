namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user information.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether a user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets the current user's permissions.
    /// </summary>
    IEnumerable<string> Permissions { get; }

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    bool HasPermission(string permissionCode);

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    bool HasAnyPermission(params string[] permissionCodes);

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    bool HasAllPermissions(params string[] permissionCodes);
}
