namespace SuperEcomManager.Application.Common.Attributes;

/// <summary>
/// Attribute to specify required permission for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}
