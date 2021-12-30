using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataStorage(this IServiceCollection services)
    {
        services.AddScoped<IBlogAudit, BlogAudit>();
        return services;
    }
}