using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration.Settings;

namespace Moonglade.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlogConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            var appSettings = configuration.GetSection(nameof(AppSettings));
            services.Configure<AppSettings>(appSettings);
            services.AddSingleton<IBlogConfig, BlogConfig>();
        }
    }
}
