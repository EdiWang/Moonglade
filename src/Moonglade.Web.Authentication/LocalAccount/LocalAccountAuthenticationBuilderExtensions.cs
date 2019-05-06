using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Model;

namespace Moonglade.Web.Authentication.LocalAccount
{
    public static class LocalAccountAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddMoongladeLocalAccount(this AuthenticationBuilder builder)
        {
            builder.AddCookie(Constants.CookieAuthSchemeName, options =>
            {
                options.AccessDeniedPath = "/admin/accessdenied";
                options.LoginPath = "/admin/signin";
                options.LogoutPath = "/admin/signout";
            });

            builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            return builder;
        }
    }
}
