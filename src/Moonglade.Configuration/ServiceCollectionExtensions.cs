using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogConfig(this IServiceCollection services)
    {
        services.AddSingleton<IBlogConfig, BlogConfig>();
        return services;
    }

    public static IServiceCollection AddAnalytics(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Analytics");
        services.Configure<AnalyticsSettings>(section);

        return services;
    }
}