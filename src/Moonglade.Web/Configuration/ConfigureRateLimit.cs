using AspNetCoreRateLimit;

namespace Moonglade.Web.Configuration;

// Setup document: https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup
public static class ConfigureRateLimit
{
    public static IServiceCollection AddRateLimit(this IServiceCollection services, IConfigurationSection rateLimitSection)
    {
        services.AddMemoryCache();

        services.Configure<IpRateLimitOptions>(rateLimitSection);
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}