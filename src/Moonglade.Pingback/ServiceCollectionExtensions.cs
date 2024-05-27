using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Moonglade.Pingback;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPingback(this IServiceCollection services)
    {
        services.AddHttpClient<IMentionSourceInspector, MentionSourceInspector>()
                .ConfigureHttpClient(p => p.Timeout = TimeSpan.FromSeconds(30))
                .AddStandardResilienceHandler();

        services.AddHttpClient<IPingbackWebRequest, PingbackWebRequest>()
                .AddStandardResilienceHandler();

        services.AddHttpClient<IPingbackSender, PingbackSender>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials })
                .AddStandardResilienceHandler();

        return services;
    }
}