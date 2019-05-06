using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Authentication.AzureAd
{
    // from https://github.com/aspnet/AspNetCore/tree/master/src/Azure/AzureAD/Authentication.AzureAD.UI/src
    // official nuget package contains MVC Views, which makes it difficult to customize UI
    // I have to download and separate the code, sad...

    public static class AzureAdAuthenticationBuilderExtensions
    {        
        public static AuthenticationBuilder AddAzureAD(this AuthenticationBuilder builder, Action<AzureAdInfo> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureAzureOptions: IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AzureAdInfo _azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdInfo> azureOptions)
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
