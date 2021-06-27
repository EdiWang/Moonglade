using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Theme
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlogTheme(this IServiceCollection services)
        {
            services.AddScoped<IThemeService, ThemeService>();
            return services;
        }
    }
}
