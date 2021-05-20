using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
