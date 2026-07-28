namespace Moonglade.Auth;

public class AuthenticationSettings
{
    public AuthenticationProvider Provider { get; set; } = AuthenticationProvider.Local;
    public TotpAuthenticationSettings Totp { get; set; } = new();
}

public class TotpAuthenticationSettings
{
    public string Issuer { get; set; } = "Moonglade";
}
