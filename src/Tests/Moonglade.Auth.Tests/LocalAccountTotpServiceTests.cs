using OtpNet;

namespace Moonglade.Auth.Tests;

public class LocalAccountTotpServiceTests
{
    [Fact]
    public void GenerateSecret_ReturnsSecretThatCanVerifyCurrentTotp()
    {
        var service = new LocalAccountTotpService();
        var secret = service.GenerateSecret();
        var currentCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

        var result = service.VerifyCode(secret, currentCode);

        Assert.True(result);
    }

    [Fact]
    public void VerifyCode_InvalidCode_ReturnsFalse()
    {
        var service = new LocalAccountTotpService();
        var secret = service.GenerateSecret();

        var result = service.VerifyCode(secret, "000000");

        Assert.False(result);
    }

    [Fact]
    public void VerifyCode_InvalidSecret_ReturnsFalse()
    {
        var service = new LocalAccountTotpService();

        var result = service.VerifyCode("not-a-secret", "123456");

        Assert.False(result);
    }

    [Fact]
    public void BuildAuthenticatorUri_IncludesIssuerAccountAndSecret()
    {
        var service = new LocalAccountTotpService();

        var result = service.BuildAuthenticatorUri("Moonglade Blog", "admin", "ABCDEF234567");

        Assert.StartsWith("otpauth://totp/Moonglade%20Blog:admin?", result);
        Assert.Contains("secret=ABCDEF234567", result);
        Assert.Contains("issuer=Moonglade%20Blog", result);
        Assert.Contains("digits=6", result);
        Assert.Contains("period=30", result);
    }
}
