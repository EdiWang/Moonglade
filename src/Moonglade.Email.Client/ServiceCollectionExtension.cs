using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Email.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddNotification(this IServiceCollection services)
    {
        services.AddScoped<IMoongladeEmailClient, AzureStorageQueueNotification>();
        return services;
    }
}