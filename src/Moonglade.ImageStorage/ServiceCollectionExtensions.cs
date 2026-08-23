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

        if (settings is null)
        {
            throw new InvalidOperationException($"ImageStorage settings cannot be null. Ensure the '{ImageStorageSection}' section exists in configuration.");
        }

        services.Configure<ImageStorageSettings>(section);

        var primaryPath = string.IsNullOrWhiteSpace(settings.FileSystemPath)
            ? FileSystemImageStorage.DefaultPath
            : settings.FileSystemPath;
        var originalPath = string.IsNullOrWhiteSpace(settings.OriginalFileSystemPath)
            ? FileSystemImageStorage.DefaultOriginalPath
            : settings.OriginalFileSystemPath;

        var imageConfiguration = FileSystemImageStorage.ResolveImageStoragePaths(primaryPath, originalPath);

        services.AddSingleton(imageConfiguration)
            .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
            .AddScoped<IFileNameGenerator, DatedGuidFileNameGenerator>();

        return services;
    }
}
