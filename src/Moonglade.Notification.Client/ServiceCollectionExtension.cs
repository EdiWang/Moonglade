using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Moonglade.Notification.Client
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddNotificationClient(this IServiceCollection services)
        {
            services.AddHttpClient<IBlogNotificationClient, NotificationClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3,
                        retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));

            return services;
        }
    }
}