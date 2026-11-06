using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moonglade.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogConfig(this IServiceCollection services)
    {
        services.AddSingleton<IBlogConfig, BlogConfig>();
        services.TryAddSingleton<ConfigurationWriteGate>();
        return services;
    }
}
