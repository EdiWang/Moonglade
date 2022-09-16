using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;
using Pzy.Qiniu;

namespace Moonglade.ImageStorage;

public class ImageStorageOptions
{
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
}

public static class ServiceCollectionExtensions
{
    private static readonly ImageStorageOptions Options = new();

    public static IServiceCollection AddImageStorage(
        this IServiceCollection services, IConfiguration configuration, Action<ImageStorageOptions> options)
    {
        options(Options);

        var section = configuration.GetSection(nameof(ImageStorage));
        var settings = section.Get<ImageStorageSettings>();
        services.Configure<ImageStorageSettings>(section);

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
                if (string.IsNullOrWhiteSpace(settings.FileSystemPath))
                {
                    throw new ArgumentNullException(nameof(settings.FileSystemPath), "FileSystemPath can not be null or empty.");
                }
                services.AddFileSystemStorage(settings.FileSystemPath);
                break;
            case "miniostorage":
                if (settings.MinioStorageSettings == null)
                {
                    throw new ArgumentNullException(nameof(settings.MinioStorageSettings), "MinioStorageSettings can not be null.");
                }
                services.AddMinioStorage(settings.MinioStorageSettings);
                break;
            case "qiniustorage":
                if (settings.QiniuStorageSettings == null)
                {
                    throw new ArgumentNullException(nameof(settings.QiniuStorageSettings), "QiniuStorageSettings can not be null.");
                }
                services.AddQiniuStorage(settings.QiniuStorageSettings);
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
        services.AddSingleton(_ => new AzureBlobConfiguration(conn, container))
                .AddSingleton<IBlogImageStorage, AzureBlobImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddFileSystemStorage(this IServiceCollection services, string fileSystemPath)
    {
        var fullPath = FileSystemImageStorage.ResolveImageStoragePath(fileSystemPath);
        services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath))
                .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddMinioStorage(this IServiceCollection services, MinioStorageSettings settings)
    {
        services.AddSingleton<IBlogImageStorage, MinioBlobImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()))
                .AddSingleton(_ => new MinioBlobConfiguration(
                    settings.EndPoint,
                    settings.AccessKey,
                    settings.SecretKey,
                    settings.BucketName,
                    settings.WithSSL));
    }

    private static void AddQiniuStorage(this IServiceCollection services, QiniuStorageSettings settings)
    {
        services.AddQiniuStorage()
                .AddScoped<IFileNameGenerator, RegularFileNameGenerator>()
                .AddSingleton<IBlogImageStorage, QiniuBlobImageStorage>()
                .AddSingleton<IMacSettings>(new MacSettings(settings.AccessKey, settings.SecretKey))
                .AddSingleton<IQiniuConfiguration>(_ => new QiniuBlobConfiguration(
                    settings.EndPoint,
                    settings.BucketName,
                    settings.WithSSL));
    }
}