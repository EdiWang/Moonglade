using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Data.MySql;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(typeof(IRepositoryBase<>), typeof(MySqlDbContextRepository<>));

        services.AddDbContext<BlogDbContext, MySqlBlogDbContext>(options => options
                .UseMySQL(connectionString, builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                })
                .EnableDetailedErrors());

        return services;
    }
}