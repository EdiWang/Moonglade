using Microsoft.Extensions.DependencyInjection;
using Moonglade.Pingback;

namespace Moonglade.Web.Configuration
{
    public static class ConfigurePingback
    {
        public static void AddPingback(this IServiceCollection services)
        {
            services.AddScoped<IPingSourceInspector, PingSourceInspector>();
            services.AddScoped<IPingbackRepository, PingbackRepository>();
            services.AddScoped<IPingbackSender, PingbackSender>();
            services.AddScoped<IPingbackService, PingbackService>();
        }
    }
}
