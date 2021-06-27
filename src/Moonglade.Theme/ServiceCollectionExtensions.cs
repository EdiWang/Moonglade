using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
