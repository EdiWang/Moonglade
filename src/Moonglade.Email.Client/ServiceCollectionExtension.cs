using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Email.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddEmailClient(this IServiceCollection services)
    {
        services.AddHttpClient<IMoongladeEmailClient, MoongladeEmailClient>()
                .AddStandardResilienceHandler();

        return services;
    }
}