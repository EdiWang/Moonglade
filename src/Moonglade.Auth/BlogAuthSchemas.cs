using Microsoft.AspNetCore.Authentication.Cookies;

namespace Moonglade.Auth;

public static class BlogAuthSchemas
{
    public const string Local = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string OpenIdConnect = "OpenIdConnect";
    public const string LocalAccountSetup = "MoongladeLocalAccountSetup";
    public const string LocalAccountTwoFactor = "MoongladeLocalAccountTwoFactor";
    public const string AdministratorPolicy = "MoongladeAdministrator";
}
