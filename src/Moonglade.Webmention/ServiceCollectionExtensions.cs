using Edi.AspNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Webmention;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebmention(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebmentionSourceRateLimitOptions>(
            configuration.GetSection("Webmention:SourceRateLimit"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IWebmentionSourceRateLimiter, WebmentionSourceRateLimiter>();
        services.AddPublicHttpClientSafety();

        services.AddHttpClient<IMentionSourceInspector, MentionSourceInspector>()
                .ConfigureHttpClient(p =>
                {
                    p.Timeout = TimeSpan.FromSeconds(30);
                    p.MaxResponseContentBufferSize = 1024 * 1024; // 1 MB
                })
                .ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<PublicHttpMessageHandlerFactory>().Create())
                .AddStandardResilienceHandler();

        services.AddHttpClient<IWebmentionSender, WebmentionSender>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<PublicHttpMessageHandlerFactory>().Create())
                .AddStandardResilienceHandler();

        services.AddHttpClient<IWebmentionRequestor, WebmentionRequestor>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<PublicHttpMessageHandlerFactory>().Create())
                .AddStandardResilienceHandler();

        return services;
    }
}
