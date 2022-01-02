using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.MySql.Infrastructure;
using Moonglade.Data.MySql.Setup;
using Moonglade.Data.Setup;
using MySqlConnector;
using System.Data;

namespace Moonglade.Data.MySql
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMySqlStorage(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IDbConnection>(_ => new MySqlConnection(connectionString));
            services.AddTransient<ISetupRunner, SetupRunnerForMySql>();
            services.AddScoped(typeof(IRepository<>), typeof(MySqlDbContextRepository<>));

            services.AddDbContext<BlogMySqlDbContext>(optionsAction => optionsAction.UseLazyLoadingProxies()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), builder =>
                  {
                      builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                  })
                .EnableDetailedErrors());

            return services;
        }
    }
}