using Microsoft.AspNetCore.Authentication;
using System;

namespace Moonglade.Auth
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddApiKeySupport(
            this AuthenticationBuilder authenticationBuilder,
            Action<ApiKeyAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme,
                options);
        }
    }
}
