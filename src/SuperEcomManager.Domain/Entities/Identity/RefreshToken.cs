using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Identity;

/// <summary>
/// Refresh token for JWT authentication.
/// Stored per-tenant to support multi-tenancy.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation property
    public User? User { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, Guid tenantId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(string? reason = null, string? replacedByToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
    }
}
