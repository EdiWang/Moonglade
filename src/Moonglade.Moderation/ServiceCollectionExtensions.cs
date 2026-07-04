using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moonglade.Moderation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentModerator(this IServiceCollection services)
    {
        services.TryAddScoped<IModerationKeywordProvider, EmptyModerationKeywordProvider>();
        services.AddScoped<ILocalModerationService, LocalModerationService>();
        services.AddScoped<IModeratorService, MoongladeModeratorService>();

        return services;
    }
}
