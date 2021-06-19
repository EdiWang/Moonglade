using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Moonglade.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreBloggingServices(this IServiceCollection services)
        {
            services.AddScoped<IBlogStatistics, BlogStatistics>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IPostManageService, PostManageService>();
            services.AddScoped<IPostQueryService, PostQueryService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ITagService, TagService>();

            return services;
        }

        public static IServiceCollection AddReleaseCheckerClient(this IServiceCollection services)
        {
            services.AddHttpClient<IReleaseCheckerClient, ReleaseCheckerClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));

            return services;
        }
    }
}
