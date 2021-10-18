using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using System.Data;

namespace Moonglade.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataStorage(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IDbConnection>(_ => new SqlConnection(connectionString));
            services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));
            services.AddDbContext<BlogDbContext>(options =>
                options.UseLazyLoadingProxies()
                    .UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            3,
                            TimeSpan.FromSeconds(30),
                            null);
                    }));
            services.AddScoped<IBlogAudit, BlogAudit>();

            return services;
        }
    }
}
