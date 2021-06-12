using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Page
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlogPage(this IServiceCollection services)
        {
            services.AddScoped<IBlogPageService, BlogPageService>();
            return services;
        }
    }
}
