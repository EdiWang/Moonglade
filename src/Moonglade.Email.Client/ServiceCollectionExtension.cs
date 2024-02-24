using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Email.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddEmailSending(this IServiceCollection services)
    {
        services.AddHttpClient<IMoongladeEmailClient, MoongladeEmailClient>();
        return services;
    }
}