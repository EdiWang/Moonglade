using System.Linq;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Moonglade.Web.Configuration
{
    // Setup document: https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup
    public static class ConfigureRateLimit
    {
        public static IServiceCollection AddRateLimit(this IServiceCollection services, IConfigurationSection rateLimitSection)
        {
            if (services.All(s => s.ServiceType != typeof(IOptions<>)))
            {
                services.AddOptions();
            }

            if (services.All(s => s.ServiceType != typeof(IMemoryCache)))
            {
                services.AddMemoryCache();
            }

            if (services.All(s => s.ServiceType != typeof(IHttpContextAccessor)))
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }

            services.Configure<IpRateLimitOptions>(rateLimitSection);
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            return services;
        }
    }
}
