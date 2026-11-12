using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moonglade.Auth.Oidc;
using Moonglade.Data;

namespace Moonglade.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OwnerAccountStore>();
        services.AddScoped<OwnerAvatarStore>();
        services.AddScoped<LocalAccountCookieEvents>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IOwnerProfileReader>(sp => sp.GetRequiredService<OwnerAccountStore>());
        services.Configure<AuthenticationSettings>(configuration.GetSection("Authentication"));
        services.AddSingleton<ILocalAccountTotpService, LocalAccountTotpService>();
        services.AddSingleton<OidcSecretProtection>();
        services.AddSingleton<OidcBindingFlow>();
        services.AddMemoryCache();
        services.AddSingleton<OidcSetupDrafts>();
        services.AddScoped(sp => new OwnerOidcStore(sp.GetRequiredService<BlogDbContext>(),
            sp.GetRequiredService<OidcSecretProtection>()));
        services.AddHttpClient(OwnerOidcHandler.BackchannelClient, client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(() => new OidcPublicBackchannel())
            .RemoveAllLoggers();

        services.AddAuthorizationBuilder()
            .AddPolicy(BlogAuthSchemas.AdministratorPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("Administrator"));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.AccessDeniedPath = "/auth/accessdenied";
                options.LoginPath = "/auth/signin";
                options.LogoutPath = "/auth/signout";
                options.EventsType = typeof(LocalAccountCookieEvents);
            })
            .AddCookie(BlogAuthSchemas.LocalAccountSetup, options =>
            {
                options.Cookie.Name = ".Moonglade.LocalAccount.Setup";
                options.LoginPath = "/auth/signin";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
                options.EventsType = typeof(LocalAccountCookieEvents);
            })
            .AddCookie(BlogAuthSchemas.LocalAccountTwoFactor, options =>
            {
                options.Cookie.Name = ".Moonglade.LocalAccount.TwoFactor";
                options.LoginPath = "/auth/signin";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
                options.EventsType = typeof(LocalAccountCookieEvents);
            })
            .AddCookie(BlogAuthSchemas.InitialAccountSetup, options =>
            {
                options.Cookie.Name = ".Moonglade.InitialAccount.Setup";
                options.LoginPath = "/auth/setup-account";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            })
            .AddCookie(OidcBindingFlow.PendingScheme, options =>
            {
                options.Cookie.Name = ".Moonglade.OidcPendingBinding";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = OidcBindingFlow.Lifetime;
                options.SlidingExpiration = false;
            })
            .AddScheme<AuthenticationSchemeOptions, OwnerOidcHandler>(OidcBindingFlow.Scheme, null);

        return services;
    }
}
