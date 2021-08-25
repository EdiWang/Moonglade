using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;

namespace Moonglade.Pingback
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPingback(this IServiceCollection services)
        {
            services.AddHttpClient<IPingSourceInspector, PingSourceInspector>()
                .ConfigureHttpClient(p => p.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<IPingbackWebRequest, PingbackWebRequest>();
            services.AddHttpClient<IPingbackSender, PingbackSender>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials });
            services.AddScoped<IPingbackService, PingbackService>();

            return services;
        }
    }
}
