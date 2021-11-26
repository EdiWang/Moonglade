using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using System.Data;

namespace Moonglade.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataStorage(this IServiceCollection services, string connectionString)
    {
        services.AddTransient<IDbConnection>(_ => new SqlConnection(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));
        services.AddSqlServer<BlogDbContext>(connectionString, options =>
        {
            options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        }, builder =>
        {
            builder.UseLazyLoadingProxies();
        });
        services.AddScoped<IBlogAudit, BlogAudit>();

        return services;
    }
}