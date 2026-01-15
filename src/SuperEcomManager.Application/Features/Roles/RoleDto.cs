namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Role data transfer object.
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public IReadOnlyList<PermissionDto> Permissions { get; set; } = Array.Empty<PermissionDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Role summary for lists.
/// </summary>
public class RoleSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

/// <summary>
/// Permission data transfer object.
/// </summary>
public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Permissions grouped by module.
/// </summary>
public class PermissionGroupDto
{
    public string Module { get; set; } = string.Empty;
    public IReadOnlyList<PermissionDto> Permissions { get; set; } = Array.Empty<PermissionDto>();
}
