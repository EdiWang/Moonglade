using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Comments.Moderators;

namespace Moonglade.Comments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddComments(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("AzureContentModerator");
        var settings = section.Get<AzureContentModeratorSettings>();

        services.Configure<AzureContentModeratorSettings>(section);

        if (settings != null &&
            !string.IsNullOrWhiteSpace(settings.Endpoint) &&
            !string.IsNullOrWhiteSpace(settings.OcpApimSubscriptionKey))
        {
            var cred = new ApiKeyServiceClientCredentials(settings.OcpApimSubscriptionKey);
            services.AddTransient<IContentModeratorClient>(_ => new ContentModeratorClient(cred)
            {
                Endpoint = settings.Endpoint
            });

            services.AddScoped<ICommentModerator, AzureContentModerator>();
        }
        else
        {
            services.AddScoped<ICommentModerator, LocalModerator>();
        }

        return services;
    }
}