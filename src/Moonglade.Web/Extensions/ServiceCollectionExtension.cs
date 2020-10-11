using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.DateTimeOps;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.Providers;
using Moonglade.Model.Settings;
using Moonglade.Pingback;
using Moonglade.Web.Filters;
using Polly;

namespace Moonglade.Web.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddBlogConfiguration(this IServiceCollection services, IConfigurationSection appSettings)
        {
            services.AddOptions();
            services.Configure<AppSettings>(appSettings);
            services.AddSingleton<IBlogConfig, BlogConfig>();
            services.AddScoped<IDateTimeResolver>(c =>
                new DateTimeResolver(c.GetService<IBlogConfig>().GeneralSettings.TimeZoneUtcOffset));
        }

        public static void AddDataStorage(this IServiceCollection services, string connectionString)
        {
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

        public static void AddBlogCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBlogCache, BlogCache>();
            services.AddScoped<DeleteSubscriptionCache>();
            services.AddScoped<DeleteSiteMapCache>();
        }

        public static void AddBlogNotification(this IServiceCollection services, ILogger logger)
        {
            services.AddHttpClient<IBlogNotificationClient, NotificationClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3, retryCount =>
                            TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                        (result, span, retryCount, context) =>
                        {
                            logger?.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {span} before next retry. Retry attempt {retryCount}/3.");
                        }));
        }

        public static void AddBlogServices(this IServiceCollection services)
        {
            var asm = Assembly.GetAssembly(typeof(BlogService));
            if (null != asm)
            {
                var types = asm.GetTypes().Where(t => t.IsClass && t.IsPublic && t.Name.EndsWith("Service"));
                foreach (var t in types)
                {
                    services.AddScoped(t, t);
                }
            }
        }

        public static void AddPingback(this IServiceCollection services)
        {
            services.AddScoped<IPingSourceInspector, PingSourceInspector>();
            services.AddScoped<IPingbackRepository, PingbackRepository>();
            services.AddScoped<IPingbackSender, PingbackSender>();
            services.AddScoped<IPingbackService, PingbackService>();
        }

        public static void AddImageStorage(
            this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var imageStorage = new ImageStorageSettings();
            configuration.Bind(nameof(ImageStorage), imageStorage);
            services.Configure<ImageStorageSettings>(configuration.GetSection(nameof(ImageStorage)));

            services.AddScoped<IFileNameGenerator>(gen => new GuidFileNameGenerator(Guid.NewGuid()));

            if (imageStorage.CDNSettings.EnableCDNRedirect)
            {
                if (string.IsNullOrWhiteSpace(imageStorage.CDNSettings.CDNEndpoint))
                {
                    throw new ArgumentNullException(nameof(imageStorage.CDNSettings.CDNEndpoint),
                        $"{nameof(imageStorage.CDNSettings.CDNEndpoint)} must be specified when {nameof(imageStorage.CDNSettings.EnableCDNRedirect)} is set to 'true'.");
                }

                // _logger.LogWarning("Images are configured to use CDN, the endpoint is out of control, use it on your own risk.");

                // Validate endpoint Url to avoid security risks
                // But it still has risks:
                // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

                var endpoint = imageStorage.CDNSettings.CDNEndpoint;
                var isValidEndpoint = endpoint.IsValidUrl(Utils.UrlScheme.Https);
                if (!isValidEndpoint)
                {
                    throw new UriFormatException("CDN Endpoint is not a valid HTTPS Url.");
                }
            }

            if (null == imageStorage.Provider)
            {
                throw new ArgumentNullException("Provider", "Provider can not be null.");
            }

            var imageStorageProvider = imageStorage.Provider.ToLower();
            if (string.IsNullOrWhiteSpace(imageStorageProvider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be empty.");
            }

            switch (imageStorageProvider)
            {
                case "azurestorage":
                    var conn = imageStorage.AzureStorageSettings.ConnectionString;
                    var container = imageStorage.AzureStorageSettings.ContainerName;
                    services.AddSingleton(s => new AzureBlobConfiguration(conn, container));
                    services.AddSingleton<IBlogImageStorage, AzureBlobImageStorage>();
                    break;
                case "filesystem":
                    var path = imageStorage.FileSystemSettings.Path;
                    var fullPath = FileSystemImageStorage.ResolveImageStoragePath(environment.ContentRootPath, path);
                    services.AddSingleton(s => new FileSystemImageConfiguration(fullPath));
                    services.AddSingleton<IBlogImageStorage, FileSystemImageStorage>();
                    break;
                default:
                    var msg = $"Provider {imageStorageProvider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
