using Microsoft.AspNetCore.Authentication.Cookies;

namespace Moonglade.Auth;

public static class BlogAuthSchemas
{
    public const string Local = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string LocalAccountSetup = "MoongladeLocalAccountSetup";
    public const string LocalAccountTwoFactor = "MoongladeLocalAccountTwoFactor";
    public const string LocalAccountRememberedDevice = "MoongladeLocalAccountRememberedDevice";
    public const string InitialAccountSetup = "MoongladeInitialAccountSetup";
    public const string AdministratorPolicy = "MoongladeAdministrator";
}
