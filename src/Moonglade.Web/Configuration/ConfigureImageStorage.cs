using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.Web.Configuration
{
    public static class ConfigureImageStorage
    {
        public static void AddImageStorage(
           this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var imageStorage = new ImageStorageSettings();
            configuration.Bind(nameof(ImageStorage), imageStorage);
            services.Configure<ImageStorageSettings>(configuration.GetSection(nameof(ImageStorage)));

            services.AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));

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
                var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
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
                    services.AddSingleton(_ => new AzureBlobConfiguration(conn, container));
                    services.AddSingleton<IBlogImageStorage, AzureBlobImageStorage>();
                    break;
                case "filesystem":
                    var path = imageStorage.FileSystemSettings.Path;
                    var fullPath = FileSystemImageStorage.ResolveImageStoragePath(environment.ContentRootPath, path);
                    services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath));
                    services.AddSingleton<IBlogImageStorage, FileSystemImageStorage>();
                    break;
                case "miniostorage":
                    var endPoint = imageStorage.MinioStorageSettings.EndPoint;
                    var accessKey = imageStorage.MinioStorageSettings.AccessKey;
                    var secretKey = imageStorage.MinioStorageSettings.SecretKey;
                    var bucketName = imageStorage.MinioStorageSettings.BucketName;
                    var withSSL = imageStorage.MinioStorageSettings.WithSSL;
                    services.AddSingleton(_ => new MinioBlobConfiguration(endPoint, accessKey, secretKey, bucketName, withSSL));
                    services.AddSingleton<IBlogImageStorage, MinioBlobImageStorage>();
                    break;
                default:
                    var msg = $"Provider {imageStorageProvider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
