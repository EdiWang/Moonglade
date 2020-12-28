using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Core;

namespace Moonglade.Web.Configuration
{
    public static class ConfigureCommentModerator
    {
        public static void AddCommentModerator(
            this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var settings = new CommentModeratorSettings();
            configuration.Bind("CommentModerator", settings);
            services.Configure<CommentModeratorSettings>(configuration.GetSection("CommentModerator"));

            if (null == settings.Provider)
            {
                throw new ArgumentNullException("Provider", "Provider can not be null.");
            }

            var provider = settings.Provider.ToLower();
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be empty.");
            }

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
