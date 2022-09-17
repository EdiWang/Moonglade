using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Moonglade.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReleaseCheckerClient(this IServiceCollection services)
    {
        services.AddHttpClient<IReleaseCheckerClient, ReleaseCheckerClient>()
                .AddTransientHttpErrorPolicy(builder =>
                builder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));

        return services;
    }
}