using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Pingback
{
    public static class ConfigurePingback
    {
        public static void AddPingback(this IServiceCollection services)
        {
            services.AddHttpClient<IPingSourceInspector, PingSourceInspector>()
                .ConfigureHttpClient(p => p.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<IPingbackRepository, PingbackRepository>();
            services.AddScoped<IPingbackWebRequest, PingbackWebRequest>();
            services.AddHttpClient<IPingbackSender, PingbackSender>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials });
            services.AddScoped<IPingbackService, PingbackService>();
        }
    }
}
