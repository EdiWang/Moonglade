using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Utils;
using System.Net.Http.Headers;

namespace Moonglade.IndexNow.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddIndexNowClient(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        var pingTargets = configurationSection.GetSection("PingTargets").Get<string[]>();
        if (null == pingTargets) return services;

        foreach (var pingTarget in pingTargets)
        {
            services.AddHttpClient(pingTarget, o =>
                    {
                        o.BaseAddress = new Uri($"https://{pingTarget}");
                        o.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        o.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Moonglade", Helper.AppVersionBasic));
                        o.DefaultRequestHeaders.Host = pingTarget;
                    })
                    .AddStandardResilienceHandler();
        }

        services.AddScoped<IIndexNowClient, IndexNowClient>();

        return services;
    }
}