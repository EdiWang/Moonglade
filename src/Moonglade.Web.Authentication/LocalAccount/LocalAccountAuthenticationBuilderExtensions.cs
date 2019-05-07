using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Web.Authentication.LocalAccount
{
    public static class LocalAccountAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddMoongladeLocalAccount(this AuthenticationBuilder builder)
        {
            builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
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
