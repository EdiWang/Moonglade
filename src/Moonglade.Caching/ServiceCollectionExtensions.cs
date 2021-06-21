using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogMemoryCache>();
        }
    }
}
