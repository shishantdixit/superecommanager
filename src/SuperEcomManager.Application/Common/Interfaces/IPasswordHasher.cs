namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure algorithm.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
