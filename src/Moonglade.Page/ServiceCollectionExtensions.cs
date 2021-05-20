using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Page
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlogPage(this IServiceCollection services)
        {
            services.AddScoped<IBlogPageService, BlogPageService>();
        }
    }
}
