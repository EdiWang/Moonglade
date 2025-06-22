﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.MySql.Infrastructure;

namespace Moonglade.Data.MySql;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(typeof(MoongladeRepository<>), typeof(MySqlDbContextRepository<>));

        services.AddDbContext<BlogDbContext, MySqlBlogDbContext>(optionsAction => optionsAction.UseLazyLoadingProxies()
            .UseMySQL(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            })
            .EnableDetailedErrors());

        return services;
    }
}