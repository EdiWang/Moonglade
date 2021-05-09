using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Pingback
{
    public static class ConfigurePingback
    {
        public static void AddPingback(this IServiceCollection services)
        {
            services.AddScoped<IPingSourceInspector, PingSourceInspector>();
            services.AddScoped<IPingbackRepository, PingbackRepository>();
            services.AddHttpClient<IPingbackSender, PingbackSender>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials });
            services.AddScoped<IPingbackService, PingbackService>();
        }
    }
}
