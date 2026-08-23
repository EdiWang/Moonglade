using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public static class ServiceCollectionExtensions
{
    private const string ImageStorageSection = nameof(ImageStorage);

    public static IServiceCollection AddImageStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ImageStorageSection);
        var settings = section.Get<ImageStorageSettings>();

        ValidateSettings(settings);
        services.Configure<ImageStorageSettings>(section);

        RegisterImageStorageProvider(services, settings);

        return services;
    }

    private static void ValidateSettings(ImageStorageSettings settings)
    {
        if (settings is null)
        {
            throw new InvalidOperationException($"ImageStorage settings cannot be null. Ensure the '{ImageStorageSection}' section exists in configuration.");
        }

        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            throw new InvalidOperationException("ImageStorage provider cannot be null or empty. Please specify a valid provider in configuration.");
        }
    }

    private static void RegisterImageStorageProvider(IServiceCollection services, ImageStorageSettings settings)
    {
        var provider = settings.Provider.ToLowerInvariant();

        switch (provider)
        {
            case "azurestorage":
                RegisterAzureStorage(services, settings.AzureStorageSettings);
                break;
            case "s3compatible":
                RegisterS3CompatibleStorage(services, settings.S3CompatibleStorageSettings);
                break;
            case "filesystem":
                RegisterFileSystemStorage(services, settings.FileSystemPath, settings.OriginalFileSystemPath);
                break;
            default:
                var supportedProviders = string.Join(", ", ["azurestorage", "filesystem", "s3compatible"]);
                throw new NotSupportedException($"Provider '{provider}' is not supported. Supported providers: {supportedProviders}");
        }
    }

    private static void RegisterAzureStorage(IServiceCollection services, AzureStorageSettings settings)
    {
        if (settings is null)
        {
            throw new InvalidOperationException("AzureStorageSettings cannot be null when using Azure Storage provider.");
        }

        ValidateAzureStorageSettings(settings);

        services.AddSingleton(_ => new AzureBlobConfiguration(
                settings.ConnectionString,
                settings.ContainerName,
                settings.SecondaryContainerName))
            .AddSingleton<IBlogImageStorage, AzureBlobImageStorage>()
            .AddScoped<IFileNameGenerator, DatedGuidFileNameGenerator>();
    }

    private static void RegisterFileSystemStorage(
        IServiceCollection services,
        string fileSystemPath,
        string originalFileSystemPath)
    {
        var primaryPath = string.IsNullOrWhiteSpace(fileSystemPath)
            ? FileSystemImageStorage.DefaultPath
            : fileSystemPath;
        var originalPath = string.IsNullOrWhiteSpace(originalFileSystemPath)
            ? FileSystemImageStorage.DefaultOriginalPath
            : originalFileSystemPath;

        if (string.IsNullOrWhiteSpace(fileSystemPath))
        {
            Console.WriteLine($"FileSystemPath is not set, using default path: {primaryPath}");
        }

        if (string.IsNullOrWhiteSpace(originalFileSystemPath))
        {
            Console.WriteLine($"OriginalFileSystemPath is not set, using default path: {originalPath}");
        }

        var imageConfiguration = FileSystemImageStorage.ResolveImageStoragePaths(primaryPath, originalPath);

        services.AddSingleton(imageConfiguration)
            .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
            .AddScoped<IFileNameGenerator, DatedGuidFileNameGenerator>();
    }

    private static void RegisterS3CompatibleStorage(IServiceCollection services, S3CompatibleStorageSettings settings)
    {
        if (settings is null)
        {
            throw new InvalidOperationException("S3CompatibleStorageSettings cannot be null when using S3-compatible storage provider.");
        }

        ValidateS3CompatibleStorageSettings(settings);

        services.AddSingleton(_ => new S3CompatibleStorageConfiguration(
                settings.ServiceUrl,
                settings.Region,
                settings.AccessKeyId,
                settings.SecretAccessKey,
                settings.BucketName,
                settings.SecondaryBucketName,
                settings.ForcePathStyle))
            .AddSingleton<IAmazonS3>(sp => S3CompatibleImageStorage.CreateClient(sp.GetRequiredService<S3CompatibleStorageConfiguration>()))
            .AddSingleton<IBlogImageStorage, S3CompatibleImageStorage>()
            .AddScoped<IFileNameGenerator, DatedGuidFileNameGenerator>();
    }

    private static void ValidateAzureStorageSettings(AzureStorageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException("Azure Storage container name cannot be null or empty.");
        }
    }

    private static void ValidateS3CompatibleStorageSettings(S3CompatibleStorageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            throw new InvalidOperationException("S3-compatible storage service URL cannot be null or empty.");
        }

        if (!Uri.TryCreate(settings.ServiceUrl, UriKind.Absolute, out var serviceUri) ||
            serviceUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("S3-compatible storage service URL must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(settings.AccessKeyId))
        {
            throw new InvalidOperationException("S3-compatible storage access key ID cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(settings.SecretAccessKey))
        {
            throw new InvalidOperationException("S3-compatible storage secret access key cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(settings.BucketName))
        {
            throw new InvalidOperationException("S3-compatible storage bucket name cannot be null or empty.");
        }
    }
}
