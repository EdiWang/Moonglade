using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Moonglade.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogAuthenticaton(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication");
        var authentication = section.Get<AuthenticationSettings>();
        services.Configure<AuthenticationSettings>(section);
        services.AddSingleton<ILocalAccountTotpService, LocalAccountTotpService>();

        switch (authentication.Provider)
        {
            case AuthenticationProvider.EntraID:
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }).AddMicrosoftIdentityWebApp(configuration.GetSection("Authentication:EntraID"));
                // Internally pass `null` to cookie options so there's no way to add `AccessDeniedPath` here.

                break;
            case AuthenticationProvider.Local:
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.AccessDeniedPath = "/auth/accessdenied";
                        options.LoginPath = "/auth/signin";
                        options.LogoutPath = "/auth/signout";
                    });
                break;
            default:
                var msg = $"Provider {authentication.Provider} is not supported.";
                throw new NotSupportedException(msg);
        }

        services.AddAuthentication()
            .AddCookie(BlogAuthSchemas.LocalAccountSetup, options =>
            {
                options.Cookie.Name = ".Moonglade.LocalAccount.Setup";
                options.LoginPath = "/auth/signin";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            })
            .AddCookie(BlogAuthSchemas.LocalAccountTwoFactor, options =>
            {
                options.Cookie.Name = ".Moonglade.LocalAccount.TwoFactor";
                options.LoginPath = "/auth/signin";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            });

        return services;
    }
}
