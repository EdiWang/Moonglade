using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddImageStorage_WithFileSystemProvider_RegistersDistinctPaths()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-image-storage-registration-tests", Guid.NewGuid().ToString("N"));
        var primaryPath = Path.Combine(tempDirectory, "primary");
        var originalPath = Path.Combine(tempDirectory, "original");
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ImageStorage:Provider"] = "filesystem",
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
            Assert.IsType<FileSystemImageStorage>(serviceProvider.GetRequiredService<IBlogImageStorage>());
            Assert.IsType<DatedGuidFileNameGenerator>(serviceProvider.GetRequiredService<IFileNameGenerator>());
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
            ["ImageStorage:Provider"] = "filesystem",
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
    public void AddImageStorage_WithS3CompatibleProvider_RegistersProviderServices()
    {
        var configuration = CreateS3Configuration();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddImageStorage(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        var storageConfiguration = serviceProvider.GetRequiredService<S3CompatibleStorageConfiguration>();
        Assert.Equal("https://storage.example.com", storageConfiguration.ServiceUrl);
        Assert.Equal("us-east-1", storageConfiguration.Region);
        Assert.Equal("primary-bucket", storageConfiguration.BucketName);
        Assert.Equal("secondary-bucket", storageConfiguration.SecondaryBucketName);
        Assert.True(storageConfiguration.ForcePathStyle);

        Assert.IsType<S3CompatibleImageStorage>(serviceProvider.GetRequiredService<IBlogImageStorage>());
        Assert.IsAssignableFrom<IAmazonS3>(serviceProvider.GetRequiredService<IAmazonS3>());
        Assert.IsType<DatedGuidFileNameGenerator>(serviceProvider.GetRequiredService<IFileNameGenerator>());
    }

    [Theory]
    [InlineData("ImageStorage:S3CompatibleStorageSettings:ServiceUrl", "")]
    [InlineData("ImageStorage:S3CompatibleStorageSettings:ServiceUrl", "not-a-url")]
    [InlineData("ImageStorage:S3CompatibleStorageSettings:AccessKeyId", "")]
    [InlineData("ImageStorage:S3CompatibleStorageSettings:SecretAccessKey", "")]
    [InlineData("ImageStorage:S3CompatibleStorageSettings:BucketName", "")]
    public void AddImageStorage_WithInvalidS3CompatibleSettings_ThrowsInvalidOperationException(string key, string value)
    {
        var configuration = CreateS3Configuration(key, value);
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddImageStorage(configuration));
    }

    [Fact]
    public void AddImageStorage_WithUnsupportedProvider_ListsS3CompatibleProvider()
    {
        var values = new Dictionary<string, string>
        {
            ["ImageStorage:Provider"] = "unknown"
        };
        var configuration = CreateConfiguration(values);
        var services = new ServiceCollection();

        var exception = Assert.Throws<NotSupportedException>(() => services.AddImageStorage(configuration));

        Assert.Contains("s3compatible", exception.Message);
    }

    private static IConfiguration CreateS3Configuration(string overrideKey = null, string overrideValue = null)
    {
        var values = new Dictionary<string, string>
        {
            ["ImageStorage:Provider"] = "s3compatible",
            ["ImageStorage:S3CompatibleStorageSettings:ServiceUrl"] = "https://storage.example.com",
            ["ImageStorage:S3CompatibleStorageSettings:Region"] = "us-east-1",
            ["ImageStorage:S3CompatibleStorageSettings:AccessKeyId"] = "access-key",
            ["ImageStorage:S3CompatibleStorageSettings:SecretAccessKey"] = "secret-key",
            ["ImageStorage:S3CompatibleStorageSettings:BucketName"] = "primary-bucket",
            ["ImageStorage:S3CompatibleStorageSettings:SecondaryBucketName"] = "secondary-bucket",
            ["ImageStorage:S3CompatibleStorageSettings:ForcePathStyle"] = "true"
        };

        if (overrideKey is not null)
        {
            values[overrideKey] = overrideValue;
        }

        return CreateConfiguration(values);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
