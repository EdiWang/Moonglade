using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.SqlServer.Infrastructure;

namespace Moonglade.Data.SqlServer;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(typeof(MoongladeRepository<>), typeof(SqlServerDbContextRepository<>));

        services.AddDbContext<BlogDbContext, SqlServerBlogDbContext>(options => options
                .UseSqlServer(connectionString, builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                }).
                EnableDetailedErrors());

        return services;
    }
}