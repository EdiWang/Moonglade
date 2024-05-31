using System.Net;
using Microsoft.Extensions.DependencyInjection;

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