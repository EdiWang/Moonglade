using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;

namespace Moonglade.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreBloggingServices(this IServiceCollection services)
        {
            services.AddScoped<IBlogStatistics, BlogStatistics>()
                    .AddScoped<IPostManageService, PostManageService>()
                    .AddScoped<IPostQueryService, PostQueryService>();

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
