using Microsoft.AspNetCore.Authentication;

namespace Moonglade.Auth
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "APIKey";
        public static string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }
}
