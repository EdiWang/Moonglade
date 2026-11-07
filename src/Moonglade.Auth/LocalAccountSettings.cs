using System.ComponentModel.DataAnnotations;
using OtpNet;
using Moonglade.Configuration;
using System.Text.Json;

namespace Moonglade.Auth;

public class LocalAccountSettings : IValidatableObject
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; }
    [Required]
    public string PasswordSalt { get; set; }
    public string TotpSecret { get; set; } = string.Empty;
    public bool IsTotpEnabled { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsTotpEnabled && string.IsNullOrWhiteSpace(TotpSecret))
            yield return new("TOTP secret is required when enabled.", [nameof(TotpSecret)]);
        if (!string.IsNullOrWhiteSpace(TotpSecret))
        {
            var valid = false;
            try
            {
                valid = Base32Encoding.ToBytes(TotpSecret.Trim().Replace(" ", string.Empty).ToUpperInvariant()).Length > 0;
            }
            catch (ArgumentException)
            {
                // Invalid persisted secrets are reported without including their value.
            }
            if (!valid) yield return new("TOTP secret is not valid Base32.", [nameof(TotpSecret)]);
        }
    }

    public static LocalAccountSettings ParseStored(string json)
    {
        try
        {
            var value = json?.FromJson<LocalAccountSettings>();
            if (value is not null && Validator.TryValidateObject(value, new ValidationContext(value), null, true)) return value;
        }
        catch (JsonException)
        {
        }
        throw new InvalidOperationException($"Stored configuration '{nameof(LocalAccountSettings)}' is invalid. Restore a valid value before restarting.");
    }

    public static LocalAccountSettings DefaultValue =>
        new()
        {
            Username = "admin",
            PasswordHash = "bXHAa7tEsZmCh1pYcHPotNlP0gaYfzIkxKuHoJnHMt0=", // admin123
            PasswordSalt = "Hq8jxngFtTEtl3UI294K7w==",
            TotpSecret = string.Empty,
            IsTotpEnabled = false
        };
}
