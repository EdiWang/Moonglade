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
                services.AddAzureStorage(settings);
                break;
            case "filesystem":
                services.AddFileSystemStorage(settings);
                break;
            case "miniostorage":
                services.AddMinioStorage(settings);
                break;
            case "qiniustorage":
                services.AddQiniuStorage(settings);
                break;
            default:
                var msg = $"Provider {provider} is not supported.";
                throw new NotSupportedException(msg);
        }

        return services;
    }

    private static void AddAzureStorage(this IServiceCollection services, ImageStorageSettings storageSettings)
    {
        if (storageSettings.AzureStorageSettings == null)
        {
            throw new ArgumentNullException(nameof(ImageStorageSettings.AzureStorageSettings), "AzureStorageSettings can not be null.");
        }

        var conn = storageSettings.AzureStorageSettings.ConnectionString;
        var container = storageSettings.AzureStorageSettings.ContainerName;
        services.AddSingleton(_ => new AzureBlobConfiguration(conn, container))
            .AddSingleton<IBlogImageStorage, AzureBlobImageStorage>()
            .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddFileSystemStorage(this IServiceCollection services, ImageStorageSettings storageSettings)
    {
        if (storageSettings.FileSystemSettings == null)
        {
            throw new ArgumentNullException(nameof(ImageStorageSettings.FileSystemSettings), "FileSystemSettings can not be null.");
        }

        var path = storageSettings.FileSystemSettings.Path;
        var fullPath = FileSystemImageStorage.ResolveImageStoragePath(path);
        services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath))
           .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
           .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));
    }

    private static void AddMinioStorage(this IServiceCollection services, ImageStorageSettings storageSettings)
    {
        if (storageSettings.MinioStorageSettings == null)
        {
            throw new ArgumentNullException(nameof(ImageStorageSettings.MinioStorageSettings), "MinioStorageSettings can not be null.");
        }

        var endPoint = storageSettings.MinioStorageSettings.EndPoint;
        var accessKey = storageSettings.MinioStorageSettings.AccessKey;
        var secretKey = storageSettings.MinioStorageSettings.SecretKey;
        var bucketName = storageSettings.MinioStorageSettings.BucketName;
        var withSSL = storageSettings.MinioStorageSettings.WithSSL;
        services.AddSingleton<IBlogImageStorage, MinioBlobImageStorage>()
            .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()))
            .AddSingleton(_ => new MinioBlobConfiguration(endPoint, accessKey, secretKey, bucketName, withSSL));
    }

    private static void AddQiniuStorage(this IServiceCollection services, ImageStorageSettings storageSettings)
    {
        if (storageSettings.QiniuStorageSettings == null)
        {
            throw new ArgumentNullException(nameof(ImageStorageSettings.QiniuStorageSettings), "QiniuStorageSettings can not be null.");
        }

        var endPoint = storageSettings.QiniuStorageSettings.EndPoint;
        var accessKey = storageSettings.QiniuStorageSettings.AccessKey;
        var secretKey = storageSettings.QiniuStorageSettings.SecretKey;
        var bucketName = storageSettings.QiniuStorageSettings.BucketName;
        var withSSL = storageSettings.QiniuStorageSettings.WithSSL;

        services.AddQiniuStorage()
            .AddScoped<IFileNameGenerator, RegularFileNameGenerator>()
            .AddSingleton<IBlogImageStorage, QiniuBlobImageStorage>()
            .AddSingleton<IMacSettings>(new MacSettings(accessKey, secretKey))
            .AddSingleton<IQiniuConfiguration>(_ => new QiniuBlobConfiguration(endPoint, bucketName, withSSL));
    }
}