using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Moderation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentModerator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IModeratorService, MoongladeModeratorService>()
                .AddStandardResilienceHandler();
        return services;
    }
}