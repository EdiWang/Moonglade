using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Moonglade.Moderation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentModerator(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ContentModeratorOptions>(configuration.GetSection("ContentModerator"));

        services.AddSingleton<ILocalModerationService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ContentModeratorOptions>>().Value;
            return new LocalModerationService(options.LocalKeywords ?? string.Empty);
        });

        services.AddScoped<IModeratorService, MoongladeModeratorService>();

        return services;
    }
}
