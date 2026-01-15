namespace SuperEcomManager.Domain.Entities.Identity;

/// <summary>
/// Join table between Role and Permission.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    public Role? Role { get; private set; }
    public Permission? Permission { get; private set; }

    private RolePermission() { } // EF Core constructor

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
