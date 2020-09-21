using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.DateTimeOps;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.Providers;
using Moonglade.Model.Settings;
using Moonglade.Pingback;

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

        public static void AddPingback(this IServiceCollection services)
        {
            services.AddScoped<IPingbackSender, PingbackSender>();
            services.AddScoped<IPingbackReceiver, PingbackReceiver>();
        }

        public static void AddImageStorage(
            this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var imageStorage = new ImageStorageSettings();
            configuration.Bind(nameof(ImageStorage), imageStorage);
            services.Configure<ImageStorageSettings>(configuration.GetSection(nameof(ImageStorage)));

            services.AddScoped<IFileNameGenerator>(gen => new GuidFileNameGenerator(Guid.NewGuid()));

            if (imageStorage.CDNSettings.GetImageByCDNRedirect)
            {
                if (string.IsNullOrWhiteSpace(imageStorage.CDNSettings.CDNEndpoint))
                {
                    throw new ArgumentNullException(nameof(imageStorage.CDNSettings.CDNEndpoint),
                        $"{nameof(imageStorage.CDNSettings.CDNEndpoint)} must be specified when {nameof(imageStorage.CDNSettings.GetImageByCDNRedirect)} is set to 'true'.");
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
                    var fullPath = Utils.ResolveImageStoragePath(environment.ContentRootPath, path);
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
