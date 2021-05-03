using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.Web.Configuration
{
    public class ImageStorageOptions
    {
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    }

    public static class ConfigureImageStorage
    {
        private static readonly ImageStorageOptions Options = new();

        public static void AddImageStorage(
           this IServiceCollection services, IConfiguration configuration, Action<ImageStorageOptions> options)
        {
            options(Options);

            var section = configuration.GetSection(nameof(ImageStorage));
            var settings = section.Get<ImageStorageSettings>();

            services.Configure<ImageStorageSettings>(section);
            services.AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));

            var provider = settings.Provider?.ToLower();
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be empty.");
            }

            switch (provider)
            {
                case "azurestorage":
                    var conn = settings.AzureStorageSettings.ConnectionString;
                    var container = settings.AzureStorageSettings.ContainerName;
                    services.AddSingleton(_ => new AzureBlobConfiguration(conn, container));
                    services.AddSingleton<IBlogImageStorage, AzureBlobImageStorage>();
                    break;
                case "filesystem":
                    var path = settings.FileSystemSettings.Path;
                    var fullPath = FileSystemImageStorage.ResolveImageStoragePath(Options.ContentRootPath, path);
                    services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath));
                    services.AddSingleton<IBlogImageStorage, FileSystemImageStorage>();
                    break;
                case "miniostorage":
                    var endPoint = settings.MinioStorageSettings.EndPoint;
                    var accessKey = settings.MinioStorageSettings.AccessKey;
                    var secretKey = settings.MinioStorageSettings.SecretKey;
                    var bucketName = settings.MinioStorageSettings.BucketName;
                    var withSSL = settings.MinioStorageSettings.WithSSL;
                    services.AddSingleton(_ => new MinioBlobConfiguration(endPoint, accessKey, secretKey, bucketName, withSSL));
                    services.AddSingleton<IBlogImageStorage, MinioBlobImageStorage>();
                    break;
                default:
                    var msg = $"Provider {provider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
