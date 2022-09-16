namespace Moonglade.Auth;

public class AuthenticationSettings
{
    public AuthenticationProvider Provider { get; set; }

    public AuthenticationSettings() => Provider = AuthenticationProvider.Local;
}