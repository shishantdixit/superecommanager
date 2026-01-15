namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

/// <summary>
/// Basic user information included in auth response.
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
