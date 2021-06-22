using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogMemoryCache>();
            return services;
        }
    }
}
