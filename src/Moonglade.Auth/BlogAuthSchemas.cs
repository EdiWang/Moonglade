using Microsoft.AspNetCore.Authentication.Cookies;

namespace Moonglade.Auth;

public static class BlogAuthSchemas
{
    public const string EntraID = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string Local = CookieAuthenticationDefaults.AuthenticationScheme;
}