using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogConfig(this IServiceCollection services)
    {
        services.AddSingleton<IBlogConfig, BlogConfig>();
        return services;
    }
}