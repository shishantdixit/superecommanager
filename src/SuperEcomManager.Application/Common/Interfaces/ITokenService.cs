using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for the specified user.
    /// </summary>
    string GenerateAccessToken(User user, Guid tenantId, string tenantSlug, IEnumerable<string> permissions);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a refresh token and returns the user ID if valid.
    /// </summary>
    Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a refresh token for a user.
    /// </summary>
    Task StoreRefreshTokenAsync(Guid userId, Guid tenantId, string refreshToken, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
