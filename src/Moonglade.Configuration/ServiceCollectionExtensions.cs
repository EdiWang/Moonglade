using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IBlogConfig, BlogConfig>();
        return services;
    }
}