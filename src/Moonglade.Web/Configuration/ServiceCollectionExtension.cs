using System;
using System.Data;
using System.Linq;
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
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.DataPorting;
using Moonglade.Foaf;
using Moonglade.FriendLink;
using Moonglade.Menus;
using Moonglade.Notification.Client;
using Moonglade.Pages;
using Moonglade.Syndication;
using Moonglade.Web.Filters;
using Moonglade.Web.SiteIconGenerator;
using Polly;
using WilderMinds.MetaWeblog;
using MetaWeblogService = Moonglade.Web.MetaWeblog.MetaWeblogService;

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
            services.AddScoped<ITZoneResolver>(c =>
                new BlogTZoneResolver(c.GetService<IBlogConfig>()?.GeneralSettings.TimeZoneUtcOffset));
        }

        public static void AddDataStorage(this IServiceCollection services, string connectionString)
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
        }

        public static void AddNotificationClient(this IServiceCollection services, ILogger logger)
        {
            services.AddHttpClient<IBlogNotificationClient, NotificationClient>()
                    .AddTransientHttpErrorPolicy(builder =>
                        builder.WaitAndRetryAsync(3,
                            retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount)), (result, span, retryCount, _) =>
                        {
                            logger?.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {span} before next retry. Retry attempt {retryCount}/3.");
                        }));
        }

        public static void AddBlogServices(this IServiceCollection services)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            var moongladeCore = asms.FirstOrDefault(p => p.FullName is not null && p.FullName.StartsWith("Moonglade.Core"));

            if (moongladeCore is not null)
            {
                var types = moongladeCore.GetTypes().Where(t => t.IsClass && t.IsPublic && t.Name.EndsWith("Service"));
                foreach (var t in types)
                {
                    // Find interface if there is one
                    var i = moongladeCore.GetTypes().FirstOrDefault(x => x.IsInterface && x.IsPublic && x.Name == $"I{t.Name}");
                    services.AddScoped(i ?? t, t);
                }
            }

            // Supporting Live Writer (MetaWeblogAPI)
            services.AddMetaWeblog<MetaWeblogService>();

            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IFriendLinkService, FriendLinkService>();
            services.AddScoped<IBlogAudit, BlogAudit>();
            services.AddScoped<ISiteIconGenerator, FileSystemIconGenerator>();
            services.AddScoped<IFoafWriter, FoafWriter>();
            services.AddScoped<IRSDWriter, BlogRSDWriter>();
            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IBlogStatistics, BlogStatistics>();
            services.AddScoped<ISyndicationService, SyndicationService>();
            services.AddScoped<IOpmlWriter, MemoryStreamOpmlWriter>();

            services.AddBlogCache();
            services.AddPingback();
        }

        private static void AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogMemoryCache>();
            services.AddScoped<ClearSubscriptionCache>();
            services.AddScoped<ClearSiteMapCache>();
        }
    }
}
