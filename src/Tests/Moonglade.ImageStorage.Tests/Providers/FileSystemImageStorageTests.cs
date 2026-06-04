using Microsoft.Extensions.Logging;
using Moonglade.ImageStorage.Providers;
using Moq;
using System.Text;

namespace Moonglade.ImageStorage.Tests.Providers;

public class FileSystemImageStorageTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-image-storage-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InsertAsync_WritesFileAndReturnsFileName()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        var bytes = Encoding.UTF8.GetBytes("image data");

        var result = await storage.InsertAsync("test.jpg", bytes);

        Assert.Equal("test.jpg", result);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(_tempDirectory, "test.jpg"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAsync_ExistingFile_ReturnsImageInfo()
    {
        Directory.CreateDirectory(_tempDirectory);
        var bytes = Encoding.UTF8.GetBytes("image data");
        await File.WriteAllBytesAsync(Path.Combine(_tempDirectory, "test.png"), bytes, TestContext.Current.CancellationToken);
        var storage = CreateStorage();

        var result = await storage.GetAsync("test.png");

        Assert.NotNull(result);
        Assert.Equal(bytes, result.ImageBytes);
        Assert.Equal("png", result.ImageExtensionName);
    }

    [Fact]
    public async Task GetAsync_MissingFile_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();

        var result = await storage.GetAsync("missing.jpg");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_RemovesFile()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "test.jpg");
        await File.WriteAllTextAsync(filePath, "image data", TestContext.Current.CancellationToken);
        var storage = CreateStorage();

        await storage.DeleteAsync("test.jpg");

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_MissingFile_DoesNotThrow()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();

        await storage.DeleteAsync("missing.jpg");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../test.jpg")]
    [InlineData("folder/test.jpg")]
    [InlineData("test..jpg")]
    public async Task Operations_WithInvalidFileName_ThrowArgumentException(string fileName)
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetAsync(fileName));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.InsertAsync(fileName, [1, 2, 3]));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(fileName));
    }

    [Fact]
    public async Task InsertSecondaryAsync_DelegatesToInsertAsync()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        var bytes = Encoding.UTF8.GetBytes("secondary");

        var result = await storage.InsertSecondaryAsync("secondary.jpg", bytes);

        Assert.Equal("secondary.jpg", result);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(_tempDirectory, "secondary.jpg"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ResolveImageStoragePath_RelativePath_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => FileSystemImageStorage.ResolveImageStoragePath("relative/path"));
    }

    [Fact]
    public void ResolveImageStoragePath_AbsolutePath_CreatesDirectoryAndReturnsFullPath()
    {
        var targetPath = Path.Combine(_tempDirectory, "nested", "images");

        var result = FileSystemImageStorage.ResolveImageStoragePath(targetPath);

        Assert.True(Directory.Exists(targetPath));
        Assert.Equal(Path.GetFullPath(targetPath), result);
    }

    private FileSystemImageStorage CreateStorage()
    {
        return new FileSystemImageStorage(new FileSystemImageConfiguration(_tempDirectory), Mock.Of<ILogger<FileSystemImageStorage>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
