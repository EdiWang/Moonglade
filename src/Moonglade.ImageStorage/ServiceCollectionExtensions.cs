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
            case "filesystem":
                RegisterFileSystemStorage(services, settings.FileSystemPath);
                break;
            default:
                var supportedProviders = string.Join(", ", ["azurestorage", "filesystem"]);
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

    private static void RegisterFileSystemStorage(IServiceCollection services, string fileSystemPath)
    {
        var path = string.IsNullOrWhiteSpace(fileSystemPath)
            ? FileSystemImageStorage.DefaultPath
            : fileSystemPath;

        if (string.IsNullOrWhiteSpace(fileSystemPath))
        {
            Console.WriteLine($"FileSystemPath is not set, using default path: {path}");
        }

        var fullPath = FileSystemImageStorage.ResolveImageStoragePath(path);

        services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath))
            .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
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
}