using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Moonglade.Moderation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentModerator(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<ContentModeratorOptions>(configuration.GetSection("ContentModerator"));

        // Register services based on provider
        services.AddSingleton<ILocalModerationService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ContentModeratorOptions>>().Value;
            return new LocalModerationService(options.LocalKeywords ?? string.Empty);
        });

        // Configure HttpClient for remote service
        services.AddHttpClient<IRemoteModerationService, RemoteModerationService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ContentModeratorOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.ApiEndpoint))
            {
                client.BaseAddress = new Uri(options.ApiEndpoint);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("x-functions-key", options.ApiKey);
                }
            }
        }).AddStandardResilienceHandler();

        // Register main service
        services.AddScoped<IModeratorService, MoongladeModeratorService>();

        return services;
    }
}