using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Moonglade.Auth
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static void AddBlogAuthenticaton(this IServiceCollection services, IConfiguration configuration)
        {
            var authentication = new AuthenticationSettings();
            configuration.Bind("Authentication", authentication);
            services.Configure<AuthenticationSettings>(configuration.GetSection("Authentication"));
            services.AddScoped<IGetApiKeyQuery, AppSettingsGetApiKeyQuery>();
            services.AddScoped<ILocalAccountService, LocalAccountService>();

            switch (authentication.Provider)
            {
                case AuthenticationProvider.AzureAD:
                    services.Configure<AzureAdOption>(option =>
                    {
                        option.CallbackPath = authentication.AzureAd.CallbackPath;
                        option.ClientId = authentication.AzureAd.ClientId;
                        option.Domain = authentication.AzureAd.Domain;
                        option.Instance = authentication.AzureAd.Instance;
                        option.TenantId = authentication.AzureAd.TenantId;
                    }).AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();

                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    }).AddOpenIdConnect().AddCookie().AddApiKeySupport(_ => { });

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
                case AuthenticationProvider.None:
                    break;
                default:
                    var msg = $"Provider {authentication.Provider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }

        private class ConfigureAzureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AzureAdOption _azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdOption> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = _azureOptions.ClientId;
                options.Authority = $"{_azureOptions.Instance}{_azureOptions.TenantId}";
                options.UseTokenLifetime = true;
                options.CallbackPath = _azureOptions.CallbackPath;
                options.RequireHttpsMetadata = false;
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
