using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Notification.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddNotification(this IServiceCollection services)
    {
        services.AddScoped<IMoongladeNotification, MoongladeNotification>();
        return services;
    }
}