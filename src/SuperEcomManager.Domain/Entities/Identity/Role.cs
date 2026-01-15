using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Identity;

/// <summary>
/// Represents a role within a tenant.
/// Roles define a set of permissions.
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { } // EF Core constructor

    public static Role Create(string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates the default system roles for a new tenant.
    /// </summary>
    public static IEnumerable<Role> CreateSystemRoles()
    {
        yield return Create("Owner", "Full access to all features and settings", true);
        yield return Create("Admin", "Administrative access with most permissions", true);
        yield return Create("Manager", "Can manage orders, shipments, and inventory", true);
        yield return Create("Operator", "Can process orders and create shipments", true);
        yield return Create("NDR Agent", "Handles NDR follow-ups and customer communication", true);
        yield return Create("Viewer", "Read-only access to data", true);
    }

    public void Update(string name, string? description)
    {
        if (IsSystem)
            throw new InvalidOperationException("Cannot modify system roles");

        Name = name?.Trim() ?? Name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }

    public void RemovePermission(Guid permissionId)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
        }
    }

    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        _rolePermissions.Clear();
        foreach (var permissionId in permissionIds)
        {
            _rolePermissions.Add(new RolePermission(Id, permissionId));
        }
    }
}
