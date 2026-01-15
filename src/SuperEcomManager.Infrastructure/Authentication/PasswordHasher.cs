using System.Security.Cryptography;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Authentication;

/// <summary>
/// Password hashing service using PBKDF2 with SHA256.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        // Combine salt and hash: [salt][hash]
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hash);

            if (hashBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            var salt = new byte[SaltSize];
            var storedHash = new byte[HashSize];

            Array.Copy(hashBytes, 0, salt, 0, SaltSize);
            Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }
}
