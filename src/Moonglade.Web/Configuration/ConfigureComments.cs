using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Comments;

namespace Moonglade.Web.Configuration
{
    public static class ConfigureComments
    {
        public static void AddComments(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICommentService, CommentService>();

            var settings = new CommentModeratorSettings();
            configuration.Bind("CommentModerator", settings);
            services.Configure<CommentModeratorSettings>(configuration.GetSection("CommentModerator"));

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
                    services.AddScoped<ICommentModerator>(_ => new AzureContentModerator(settings.AzureContentModeratorSettings));
                    break;
                default:
                    var msg = $"Provider {provider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
