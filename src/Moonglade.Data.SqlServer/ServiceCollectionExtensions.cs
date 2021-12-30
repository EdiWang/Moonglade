using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using System.Data;
using Moonglade.Data.SqlServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Setup;
using Moonglade.Data.Setup.SqlServer;

namespace Moonglade.Data.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlServerStorage(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IDbConnection>(_ => new SqlConnection(connectionString));
            services.AddTransient<ISetupRunner, SetupRunnerForSqlServer>();
            services.AddScoped(typeof(IRepository<>), typeof(SqlServerDbContextRepository<>));

            services.AddDbContext<BlogSqlServerDbContext>(options =>
            options.UseLazyLoadingProxies()
                   .UseSqlServer(connectionString, builder =>
                   {
                       builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                   }).
                   EnableDetailedErrors());

            services.AddDataStorage();

            return services;
        }
    }
}