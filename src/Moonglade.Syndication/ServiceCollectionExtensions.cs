using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Syndication
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSyndication(this IServiceCollection services)
        {
            services.AddScoped<ISyndicationService, SyndicationService>();
            services.AddScoped<IOpmlWriter, StringOpmlWriter>();
        }
    }
}
