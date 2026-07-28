using OtpNet;

namespace Moonglade.Auth;

public interface ILocalAccountTotpService
{
    string GenerateSecret();
    bool VerifyCode(string secret, string code);
    string BuildAuthenticatorUri(string issuer, string accountName, string secret);
}

public class LocalAccountTotpService : ILocalAccountTotpService
{
    private const int SecretByteCount = 20;
    private const int CodeDigits = 6;
    private const int PeriodSeconds = 30;

    public string GenerateSecret() =>
        Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(SecretByteCount));

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim().Replace(" ", string.Empty);
        if (normalizedCode.Length != CodeDigits || normalizedCode.Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        try
        {
            var totp = new Totp(
                Base32Encoding.ToBytes(NormalizeSecret(secret)),
                step: PeriodSeconds,
                mode: OtpHashMode.Sha1,
                totpSize: CodeDigits);

            return totp.VerifyTotp(
                normalizedCode,
                out _,
                VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string BuildAuthenticatorUri(string issuer, string accountName, string secret)
    {
        var normalizedIssuer = string.IsNullOrWhiteSpace(issuer) ? "Moonglade" : issuer.Trim();
        var normalizedAccountName = string.IsNullOrWhiteSpace(accountName) ? "admin" : accountName.Trim();
        var normalizedSecret = NormalizeSecret(secret);

        return "otpauth://totp/"
            + $"{Uri.EscapeDataString(normalizedIssuer)}:{Uri.EscapeDataString(normalizedAccountName)}"
            + $"?secret={Uri.EscapeDataString(normalizedSecret)}"
            + $"&issuer={Uri.EscapeDataString(normalizedIssuer)}"
            + $"&digits={CodeDigits}"
            + $"&period={PeriodSeconds}";
    }

    private static string NormalizeSecret(string secret) =>
        secret.Trim().Replace(" ", string.Empty).ToUpperInvariant();
}
