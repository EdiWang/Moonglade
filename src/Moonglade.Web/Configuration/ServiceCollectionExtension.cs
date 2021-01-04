using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.DataPorting;
using Moonglade.DateTimeOps;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Syndication;
using Moonglade.Web.Filters;
using Polly;
using SiteIconGenerator;

namespace Moonglade.Web.Configuration
{
    public static class ServiceCollectionExtension
    {
        public static void AddBlogConfiguration(this IServiceCollection services, IConfigurationSection appSettings)
        {
            services.AddFeatureManagement();
            services.AddOptions();
            services.Configure<AppSettings>(appSettings);
            services.AddSingleton<IBlogConfig, BlogConfig>();
            services.AddScoped<IDateTimeResolver>(c =>
                new DateTimeResolver(c.GetService<IBlogConfig>()?.GeneralSettings.TimeZoneUtcOffset));
        }

        public static void AddDataStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var connStr = configuration.GetConnectionString(Constants.DbConnectionName);

            services.AddTransient<IDbConnection>(_ => new SqlConnection(connStr));
            services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));
            services.AddDbContext<BlogDbContext>(options =>
                options.UseLazyLoadingProxies()
                    .UseSqlServer(connStr, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            3,
                            TimeSpan.FromSeconds(30),
                            null);
                    }));
        }

        public static void AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogMemoryCache>();
            services.AddScoped<ClearSubscriptionCache>();
            services.AddScoped<ClearSiteMapCache>();
        }

        public static void AddBlogNotification(this IServiceCollection services, ILogger logger)
        {
            services.AddHttpClient<IBlogNotificationClient, NotificationClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3, retryCount =>
                            TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                        (result, span, retryCount, _) =>
                        {
                            logger?.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {span} before next retry. Retry attempt {retryCount}/3.");
                        }));
        }

        public static void AddBlogServices(this IServiceCollection services)
        {
            var asm = Assembly.GetAssembly(typeof(BlogService));
            if (asm is not null)
            {
                var types = asm.GetTypes().Where(t => t.IsClass && t.IsPublic && t.Name.EndsWith("Service"));
                foreach (var t in types)
                {
                    services.AddScoped(t, t);
                }
            }

            services.AddScoped<IBlogAudit, BlogAudit>();
            services.AddScoped<ISiteIconGenerator, FileSystemIconGenerator>();
            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IBlogStatistics, BlogStatistics>();
            services.AddScoped<ISyndicationService, SyndicationService>();
            services.AddScoped<IMemoryStreamOpmlWriter, MemoryStreamOpmlWriter>();
        }
    }
}
