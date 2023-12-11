﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Moonglade.Data.Context;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.SqlServer.Infrastructure;

namespace Moonglade.Data.SqlServer;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(typeof(IRepository<>), typeof(SqlServerDbContextRepository<>));

        services.AddDbContext<SqlServerBlogDbContext>(options =>
            options.UseLazyLoadingProxies()
                .UseSqlServer(connectionString, builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                }).
                EnableDetailedErrors());
        services.AddDbContext<Moonglade1>(options =>
            options.UseLazyLoadingProxies()
                .UseSqlServer(connectionString, builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                }).EnableDetailedErrors());

        return services;
    }
}