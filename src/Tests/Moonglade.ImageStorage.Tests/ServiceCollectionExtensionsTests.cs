using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.ImageStorage.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddImageStorage_WithConfiguredPaths_RegistersSharedStorageForRequestAndBackgroundScopes()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-image-storage-registration-tests", Guid.NewGuid().ToString("N"));
        var primaryPath = Path.Combine(tempDirectory, "primary");
        var originalPath = Path.Combine(tempDirectory, "original");
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ImageStorage:FileSystemPath"] = primaryPath,
            ["ImageStorage:OriginalFileSystemPath"] = originalPath
        });
        var services = new ServiceCollection();
        services.AddLogging();

        try
        {
            services.AddImageStorage(configuration);
            using var serviceProvider = services.BuildServiceProvider();

            var storageConfiguration = serviceProvider.GetRequiredService<FileSystemImageConfiguration>();
            Assert.Equal(Path.GetFullPath(primaryPath), storageConfiguration.PrimaryPath);
            Assert.Equal(Path.GetFullPath(originalPath), storageConfiguration.OriginalPath);
            var storage = serviceProvider.GetRequiredService<FileSystemImageStorage>();
            using var scope = serviceProvider.CreateScope();
            var backgroundStorage = scope.ServiceProvider.GetRequiredService<FileSystemImageStorage>();
            Assert.Same(storage, backgroundStorage);
            byte[] imageBytes = [1, 2, 3];
            await storage.InsertAsync("photo.png", imageBytes);
            await backgroundStorage.InsertOriginalAsync("photo-origin.png", imageBytes);
            Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(primaryPath, "photo.png"), TestContext.Current.CancellationToken));
            Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(originalPath, "photo-origin.png"), TestContext.Current.CancellationToken));
            Assert.Null(await storage.OpenReadAsync("photo-origin.png"));
            Assert.IsType<DatedGuidFileNameGenerator>(serviceProvider.GetRequiredService<DatedGuidFileNameGenerator>());
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddImageStorage_WithSameFileSystemPaths_ThrowsInvalidOperationException()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-image-storage-registration-tests", Guid.NewGuid().ToString("N"));
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ImageStorage:FileSystemPath"] = tempDirectory,
            ["ImageStorage:OriginalFileSystemPath"] = Path.Combine(tempDirectory, ".")
        });
        var services = new ServiceCollection();

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => services.AddImageStorage(configuration));

            Assert.Contains("must not overlap", exception.Message);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddImageStorage_WithoutImageStorageSection_ThrowsInvalidOperationException()
    {
        var configuration = CreateConfiguration([]);
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddImageStorage(configuration));

        Assert.Contains("ImageStorage", exception.Message);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
