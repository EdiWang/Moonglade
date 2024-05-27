using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Mention.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMentionCommon(this IServiceCollection services)
    {
        services.AddHttpClient<IMentionSourceInspector, MentionSourceInspector>()
                .ConfigureHttpClient(p => p.Timeout = TimeSpan.FromSeconds(30))
                .AddStandardResilienceHandler();

        return services;
    }
}