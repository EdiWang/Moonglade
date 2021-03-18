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

            var imageStorage = new ImageStorageSettings();
            configuration.Bind(nameof(ImageStorage), imageStorage);
            services.Configure<ImageStorageSettings>(configuration.GetSection(nameof(ImageStorage)));

            services.AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));

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
                    var fullPath = FileSystemImageStorage.ResolveImageStoragePath(Options.ContentRootPath, path);
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
