using System;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Comments
{
    public static class ConfigureComments
    {
        public static void AddComments(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICommentService, CommentService>();

            var section = configuration.GetSection("CommentModerator");
            var settings = section.Get<CommentModeratorSettings>();

            services.Configure<CommentModeratorSettings>(section);

            if (string.IsNullOrWhiteSpace(settings.Provider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be null.");
            }

            var provider = settings.Provider.ToLower();

            switch (provider)
            {
                case "local":
                    services.AddScoped<ICommentModerator, LocalWordFilterModerator>();
                    break;
                case "azure":
                    var cred = new ApiKeyServiceClientCredentials(settings.AzureContentModeratorSettings.OcpApimSubscriptionKey);
                    services.AddTransient<IContentModeratorClient>(_ => new ContentModeratorClient(cred)
                    {
                        Endpoint = settings.AzureContentModeratorSettings.Endpoint
                    });

                    services.AddScoped<ICommentModerator, AzureContentModerator>();
                    break;
                default:
                    var msg = $"Provider {provider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
