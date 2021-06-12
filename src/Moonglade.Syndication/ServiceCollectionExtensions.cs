using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Syndication
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSyndication(this IServiceCollection services)
        {
            services.AddScoped<ISyndicationService, SyndicationService>();
            services.AddScoped<IOpmlWriter, StringOpmlWriter>();

            return services;
        }
    }
}
