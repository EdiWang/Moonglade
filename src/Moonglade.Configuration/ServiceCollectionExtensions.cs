using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moonglade.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogConfig(this IServiceCollection services)
    {
        services.AddSingleton<BlogConfig>();
        services.AddSingleton<IBlogConfig>(sp => sp.GetRequiredService<BlogConfig>());
        services.TryAddSingleton<ConfigurationWriteGate>();
        return services;
    }
}
