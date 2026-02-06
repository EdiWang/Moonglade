using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.PostgreSql.Infrastructure;

namespace Moonglade.Data.PostgreSql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSqlStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(typeof(IRepositoryBase<>), typeof(PostgreSqlDbContextRepository<>));

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<BlogDbContext, PostgreSqlBlogDbContext>(options => options
            .EnableDetailedErrors()
            .UseNpgsql(connectionString, options =>
            {
                options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            }));

        return services;
    }
}