using Microsoft.Extensions.DependencyInjection;
using Moonglade.Caching.Filters;

namespace Moonglade.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogMemoryCache>();
            services.AddScoped<ClearSubscriptionCache>();
            services.AddScoped<ClearSiteMapCache>();
        }
    }
}
