using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Moonglade.Webmention;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebmention(this IServiceCollection services)
    {
        services.AddHttpClient<IMentionSourceInspector, MentionSourceInspector>()
                .ConfigureHttpClient(p =>
                {
                    p.Timeout = TimeSpan.FromSeconds(30);
                    p.MaxResponseContentBufferSize = 1024 * 1024; // 1 MB
                })
                .AddStandardResilienceHandler();

        services.AddHttpClient<IWebmentionSender, WebmentionSender>()
                .AddStandardResilienceHandler();

        services.AddHttpClient<IWebmentionRequestor, WebmentionRequestor>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials })
                .AddStandardResilienceHandler();

        return services;
    }
}