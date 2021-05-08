using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.DataPorting;
using Moonglade.FriendLink;
using Moonglade.Menus;
using Moonglade.Notification.Client;
using Moonglade.Pages;
using Moonglade.Pingback;
using Moonglade.Syndication;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using Moonglade.Web.Models;
using WilderMinds.MetaWeblog;

namespace Moonglade.Web.Configuration
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtension
    {
        public static void AddBlogConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SiteMapSettings>(configuration.GetSection("SiteMap"));
            services.Configure<List<BlogTheme>>(configuration.GetSection("Themes"));
            services.Configure<List<ManifestIcon>>(configuration.GetSection("ManifestIcons"));
            services.Configure<Dictionary<string, string>>(configuration.GetSection("TagNormalization"));

            services.AddFeatureManagement();
            services.AddOptions();
            var appSettings = configuration.GetSection(nameof(AppSettings));
            services.Configure<AppSettings>(appSettings);
            services.AddSingleton<IBlogConfig, BlogConfig>();
            services.AddScoped<ITimeZoneResolver>(c =>
                new BlogTimeZoneResolver(c.GetService<IBlogConfig>()?.GeneralSettings.TimeZoneUtcOffset));
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
            services.AddScoped<IBlogPageService, BlogPageService>();
            services.AddScoped<IFriendLinkService, FriendLinkService>();
            services.AddScoped<IBlogAudit, BlogAudit>();
            services.AddScoped<IFoafWriter, FoafWriter>();
            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IBlogStatistics, BlogStatistics>();
            services.AddScoped<ISyndicationService, SyndicationService>();
            services.AddScoped<IOpmlWriter, StringOpmlWriter>();
            services.AddScoped<ValidateCaptcha>();

            services.AddBlogCache();
            services.AddPingback();
            services.AddNotificationClient();
            services.AddReleaseCheckerClient();
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
