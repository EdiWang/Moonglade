using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Moonglade.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogAuthenticaton(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication");
        var authentication = section.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
        var oidc = authentication.OpenIdConnect ?? new OpenIdConnectAuthenticationSettings();

        services.AddSingleton<IValidateOptions<AuthenticationSettings>, AuthenticationSettingsValidator>();
        services.AddOptions<AuthenticationSettings>()
            .Bind(section)
            .ValidateOnStart();
        services.AddSingleton<ILocalAccountTotpService, LocalAccountTotpService>();

        switch (authentication.Provider)
        {
            case AuthenticationProvider.OpenIdConnect:
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = BlogAuthSchemas.OpenIdConnect;
                })
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureApplicationCookie)
                    .AddOpenIdConnect(BlogAuthSchemas.OpenIdConnect, options =>
                    {
                        options.Authority = oidc.Authority;
                        options.ClientId = oidc.ClientId;
                        options.ClientSecret = oidc.ClientSecret;
                        options.CallbackPath = oidc.CallbackPath;
                        options.SignedOutCallbackPath = oidc.SignedOutCallbackPath;
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.UsePkce = true;
                        options.RequireHttpsMetadata = true;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.SaveTokens = false;
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.NameClaimType = oidc.NameClaimType;

                        options.Scope.Clear();
                        foreach (var scope in oidc.Scopes ?? [])
                        {
                            options.Scope.Add(scope);
                        }
                    });
                break;

            case AuthenticationProvider.Local:
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureApplicationCookie);
                break;

            default:
                var msg = $"Provider {authentication.Provider} is not supported.";
                throw new NotSupportedException(msg);
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(BlogAuthSchemas.AdministratorPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();

                if (authentication.Provider == AuthenticationProvider.Local)
                {
                    policy.RequireRole("Administrator");
                    return;
                }

                var allowedSubjects = (oidc.AllowedSubjects ?? [])
                    .ToHashSet(StringComparer.Ordinal);
                policy.RequireAssertion(context =>
                    context.User.FindAll("sub").Any(claim => allowedSubjects.Contains(claim.Value)));
            });

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

    private static void ConfigureApplicationCookie(CookieAuthenticationOptions options)
    {
        options.AccessDeniedPath = "/auth/accessdenied";
        options.LoginPath = "/auth/signin";
        options.LogoutPath = "/auth/signout";
    }
}
