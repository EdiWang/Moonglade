using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(ImageStorage));
        var settings = section.Get<ImageStorageSettings>();
        services.Configure<ImageStorageSettings>(section);

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings), "ImageStorage settings cannot be null.");
        }

        var provider = settings.Provider?.ToLower();
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentNullException("Provider", "Provider can not be empty.");
        }

        switch (provider)
        {
            case "azurestorage":
                if (settings.AzureStorageSettings == null)
                {
                    throw new ArgumentNullException(nameof(settings.AzureStorageSettings), "AzureStorageSettings can not be null.");
                }
                services.AddAzureStorage(settings.AzureStorageSettings);
                break;
            case "filesystem":
                string path = settings.FileSystemPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = FileSystemImageStorage.DefaultPath;
                    Console.WriteLine($"FileSystemPath is not set, using default path: {path}");
                }
                services.AddFileSystemStorage(path);
                break;
            case "miniostorage":
                if (settings.MinioStorageSettings == null)
                {
                    throw new ArgumentNullException(nameof(settings.MinioStorageSettings), "MinioStorageSettings can not be null.");
                }
                services.AddMinioStorage(settings.MinioStorageSettings);
                break;
            default:
                var msg = $"Provider {provider} is not supported.";
                throw new NotSupportedException(msg);
        }

        return services;
    }

    private static void AddAzureStorage(this IServiceCollection services, AzureStorageSettings settings)
    {
        var conn = settings.ConnectionString;
        var container = settings.ContainerName;
        var secondaryContainer = settings.SecondaryContainerName;
        services.AddSingleton(_ => new AzureBlobConfiguration(conn, container, secondaryContainer))
                .AddSingleton<IBlogImageStorage, AzureBlobImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new DatedGuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddFileSystemStorage(this IServiceCollection services, string fileSystemPath)
    {
        var fullPath = FileSystemImageStorage.ResolveImageStoragePath(fileSystemPath);
        services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath))
                .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new DatedGuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddMinioStorage(this IServiceCollection services, MinioStorageSettings settings)
    {
        services.AddSingleton<IBlogImageStorage, MinioBlobImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new DatedGuidFileNameGenerator(Guid.NewGuid()))
                .AddSingleton(_ => new MinioBlobConfiguration(
                    settings.EndPoint,
                    settings.AccessKey,
                    settings.SecretKey,
                    settings.BucketName,
                    settings.SecondaryBucketName,
                    settings.WithSSL));
    }
}