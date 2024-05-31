using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Moonglade.Webmention;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebmention(this IServiceCollection services)
    {
        services.AddHttpClient<IWebmentionSender, WebmentionSender>()
                .AddStandardResilienceHandler();

        services.AddHttpClient<IWebmentionRequestor, WebmentionRequestor>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials })
                .AddStandardResilienceHandler();

        return services;
    }
}