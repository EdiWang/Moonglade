using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Moonglade.Web.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static void AddMoongladeAuthenticaton(this IServiceCollection services, AuthenticationSettings authenticationSettings)
        {
            switch (authenticationSettings.Provider)
            {
                case AuthenticationProvider.AzureAD:
                    services.Configure<AzureAdOption>(option =>
                    {
                        option.CallbackPath = authenticationSettings.AzureAd.CallbackPath;
                        option.ClientId = authenticationSettings.AzureAd.ClientId;
                        option.Domain = authenticationSettings.AzureAd.Domain;
                        option.Instance = authenticationSettings.AzureAd.Instance;
                        option.TenantId = authenticationSettings.AzureAd.TenantId;
                    }).AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();

                    services.AddAuthentication(sharedOptions =>
                    {
                        sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    }).AddOpenIdConnect().AddCookie();

                    break;
                case AuthenticationProvider.Local:
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                            {
                                options.AccessDeniedPath = "/admin/accessdenied";
                                options.LoginPath = "/admin/signin";
                                options.LogoutPath = "/admin/signout";
                            });

                    break;
                case AuthenticationProvider.None:
                    break;
                default:
                    var msg = $"Provider {authenticationSettings.Provider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }

        private class ConfigureAzureOptions: IConfigureNamedOptions<OpenIdConnectOptions>
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
