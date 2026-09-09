using System.Security.Cryptography;
using System.Text;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToHexString(hash), Convert.ToHexString(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromHexString(salt);
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(computed, Convert.FromHexString(hash));
    }
}
