namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Lightweight DTO for user list items.
/// </summary>
public record UserListDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public bool EmailVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<string> Roles { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Full DTO for user details.
/// </summary>
public record UserDetailDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public bool EmailVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LockoutEndsAt { get; init; }
    public bool IsLockedOut { get; init; }
    public List<UserRoleDto> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for role assigned to a user.
/// </summary>
public record UserRoleDto
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public DateTime AssignedAt { get; init; }
    public Guid? AssignedBy { get; init; }
    public string? AssignedByName { get; init; }
}

/// <summary>
/// DTO for role information.
/// </summary>
public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public int UserCount { get; init; }
    public List<PermissionDto> Permissions { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Lightweight DTO for role list items.
/// </summary>
public record RoleListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public int UserCount { get; init; }
    public int PermissionCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for permission information.
/// </summary>
public record PermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>
/// Filter parameters for users query.
/// </summary>
public record UserFilterDto
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public Guid? RoleId { get; init; }
    public bool? EmailVerified { get; init; }
    public DateTime? LastLoginFrom { get; init; }
    public DateTime? LastLoginTo { get; init; }
}

/// <summary>
/// Sort options for users.
/// </summary>
public enum UserSortBy
{
    Name,
    Email,
    CreatedAt,
    LastLoginAt,
    Status
}

/// <summary>
/// DTO for user invitation.
/// </summary>
public record UserInvitationDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> InvitedRoles { get; init; } = new();
    public DateTime InvitedAt { get; init; }
    public Guid InvitedBy { get; init; }
    public string InvitedByName { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
}

/// <summary>
/// DTO for user activity log entry.
/// </summary>
public record UserActivityDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// DTO for user statistics.
/// </summary>
public record UserStatsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    public int VerifiedUsers { get; init; }
    public int UsersLoggedInToday { get; init; }
    public int UsersLoggedInThisWeek { get; init; }
    public int UsersLoggedInThisMonth { get; init; }
    public Dictionary<string, int> UsersByRole { get; init; } = new();
    public List<RecentUserDto> RecentUsers { get; init; } = new();
}

/// <summary>
/// DTO for recently active user.
/// </summary>
public record RecentUserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime? LastLoginAt { get; init; }
}

/// <summary>
/// DTO for creating a user invitation.
/// </summary>
public record CreateInvitationDto
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<Guid> RoleIds { get; init; } = new();
}

/// <summary>
/// DTO for updating user profile.
/// </summary>
public record UpdateUserProfileDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
}
