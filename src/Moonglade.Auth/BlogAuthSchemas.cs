using Microsoft.AspNetCore.Authentication.Cookies;

namespace Moonglade.Auth
{
    public static class BlogAuthSchemas
    {
        public const string All = "Cookies,APIKey";
        public const string Api = ApiKeyAuthenticationOptions.DefaultScheme;
        public const string AzureAD = CookieAuthenticationDefaults.AuthenticationScheme;
        public const string Local = CookieAuthenticationDefaults.AuthenticationScheme;
    }
}
