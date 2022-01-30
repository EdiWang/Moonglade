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
        services.AddScoped<IGetApiKeyQuery, AppSettingsGetApiKeyQuery>();

        switch (authentication.Provider)
        {
            case AuthenticationProvider.AzureAD:
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }).AddApiKeySupport(_ => { }).AddMicrosoftIdentityWebApp(configuration.GetSection("Authentication:AzureAd"));

                break;
            case AuthenticationProvider.Local:
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.AccessDeniedPath = "/auth/accessdenied";
                        options.LoginPath = "/auth/signin";
                        options.LogoutPath = "/auth/signout";
                    }).AddApiKeySupport(_ => { });
                break;
            default:
                var msg = $"Provider {authentication.Provider} is not supported.";
                throw new NotSupportedException(msg);
        }

        return services;
    }
}