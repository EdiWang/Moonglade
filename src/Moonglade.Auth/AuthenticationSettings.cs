namespace Moonglade.Auth;

public class AuthenticationSettings
{
    public AuthenticationProvider Provider { get; set; } = AuthenticationProvider.Local;
    public TotpAuthenticationSettings Totp { get; set; } = new();
    public OpenIdConnectAuthenticationSettings OpenIdConnect { get; set; } = new();
}

public class TotpAuthenticationSettings
{
    public string Issuer { get; set; } = "Moonglade";
    public bool Required { get; set; } = true;
}

public class OpenIdConnectAuthenticationSettings
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
    public string NameClaimType { get; set; } = "name";
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
    public string[] AllowedSubjects { get; set; } = [];
}
