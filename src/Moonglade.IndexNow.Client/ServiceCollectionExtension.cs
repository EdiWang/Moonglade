using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.IndexNow.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddIndexNowClient(this IServiceCollection services)
    {
        services.AddHttpClient<IIndexNowClient, IndexNowClient>()
                .AddStandardResilienceHandler();

        return services;
    }
}