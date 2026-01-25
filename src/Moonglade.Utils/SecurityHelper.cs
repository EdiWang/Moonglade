using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Moonglade.Utils;

public static class SecurityHelper
{
    // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-6.0
    // This is not secure, but better than nothing.
    public static string HashPassword(string clearPassword, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);

        // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: clearPassword!,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return hashed;
    }

    public static string GenerateSalt()
    {
        // Generate a 128-bit salt using a sequence of cryptographically strong random bytes.
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes
        return Convert.ToBase64String(salt);
    }
}
