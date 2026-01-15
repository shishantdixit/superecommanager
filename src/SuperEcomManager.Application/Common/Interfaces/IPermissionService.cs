namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for permission checking.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    Task<bool> HasPermissionAsync(string permissionCode);

    /// <summary>
    /// Checks if a specific user has the specified permission.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode);

    /// <summary>
    /// Gets all permissions for a user.
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Gets all permissions for a role.
    /// </summary>
    Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId);

    /// <summary>
    /// Authorizes the current user for a permission.
    /// Throws ForbiddenAccessException if not authorized.
    /// </summary>
    Task AuthorizeAsync(string permissionCode);

    /// <summary>
    /// Authorizes the current user for any of the specified permissions.
    /// Throws ForbiddenAccessException if not authorized.
    /// </summary>
    Task AuthorizeAnyAsync(params string[] permissionCodes);
}
