namespace Moonglade.Auth;

public class AuthenticationSettings
{
    public TotpAuthenticationSettings Totp { get; set; } = new();
}

public class TotpAuthenticationSettings
{
    public string Issuer { get; set; } = "Moonglade";
    public bool Required { get; set; } = true;
}
