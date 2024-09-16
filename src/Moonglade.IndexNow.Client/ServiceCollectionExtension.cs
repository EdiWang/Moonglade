using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Utils;

namespace Moonglade.IndexNow.Client;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddIndexNowClient(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        var pingTargets = configurationSection.GetSection("PingTargets").Get<string[]>();

        foreach (var pingTarget in pingTargets)
        {
            services.AddHttpClient(pingTarget, o =>
                    {
                        o.BaseAddress = new Uri(pingTarget);
                        o.DefaultRequestHeaders.Add("User-Agent", $"Moonglade/{Helper.AppVersionBasic}");
                        o.DefaultRequestHeaders.Add("ContentType", "application/json");
                        o.DefaultRequestHeaders.Host = pingTarget;
                    })
                    .AddStandardResilienceHandler();
        }

        return services;
    }
}