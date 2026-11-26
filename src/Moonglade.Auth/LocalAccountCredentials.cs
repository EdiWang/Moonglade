#nullable enable

using Edi.AspNetCore.Utils;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Moonglade.Auth;

internal static class LocalAccountCredentials
{
    public static (string Email, string NormalizedEmail) NormalizeEmail(string email, string requiredMessage)
    {
        if (email is null) throw new ValidationException(requiredMessage);
        var address = email.Trim();
        var context = new ValidationContext(typeof(LocalAccountCredentials)) { DisplayName = "Email" };
        Validator.ValidateValue(address, context, [new RequiredAttribute(), new EmailAddressAttribute(), new StringLengthAttribute(254)]);
        var normalized = address.ToUpperInvariant();
        Validator.ValidateValue(normalized, context, [new StringLengthAttribute(254)]);
        return (address, normalized);
    }

    public static void ValidatePassword(string password) =>
        Validator.ValidateValue(password, new ValidationContext(typeof(LocalAccountCredentials)) { DisplayName = "Password" }, [
            new RequiredAttribute(), new RegularExpressionAttribute(UpdateLocalAccountPasswordRequest.PasswordPattern)
            {
                ErrorMessage = UpdateLocalAccountPasswordRequest.PasswordRequirements
            }
        ]);

    public static (string Salt, string Hash) HashPassword(string password)
    {
        var salt = SecurityHelper.GenerateSalt();
        return (salt, SecurityHelper.HashPassword(password, salt));
    }

    public static bool PasswordMatches(BlogAccountEntity account, string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (account.PasswordHash is not { } hash || account.PasswordSalt is not { } salt)
            throw new InvalidOperationException("The initialized account has incomplete credentials.");
        var actual = Convert.FromBase64String(SecurityHelper.HashPassword(password.Trim(), salt));
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(hash));
    }
}
