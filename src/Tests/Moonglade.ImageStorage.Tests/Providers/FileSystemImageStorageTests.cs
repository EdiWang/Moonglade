using Microsoft.Extensions.Logging;
using Moonglade.ImageStorage.Providers;
using Moq;
using System.Text;

namespace Moonglade.ImageStorage.Tests.Providers;

public class FileSystemImageStorageTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-image-storage-tests", Guid.NewGuid().ToString("N"));
    private string PrimaryDirectory => Path.Combine(_tempDirectory, "primary");
    private string OriginalDirectory => Path.Combine(_tempDirectory, "original");

    [Fact]
    public async Task InsertAsync_WritesFileAndReturnsFileName()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        var bytes = Encoding.UTF8.GetBytes("image data");

        var result = await storage.InsertAsync("test.jpg", bytes);

        Assert.Equal("test.jpg", result);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(PrimaryDirectory, "test.jpg"), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertOperations_NullImageBytes_ThrowArgumentNullException(bool insertOriginal)
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        Func<Task<string>> operation = insertOriginal
            ? () => storage.InsertOriginalAsync("test.jpg", null!)
            : () => storage.InsertAsync("test.jpg", null!);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(operation);

        Assert.Equal("imageBytes", exception.ParamName);
        Assert.Empty(Directory.GetFiles(insertOriginal ? OriginalDirectory : PrimaryDirectory));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertOperations_EmptyImageBytes_ThrowArgumentException(bool insertOriginal)
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        Func<Task<string>> operation = insertOriginal
            ? () => storage.InsertOriginalAsync("test.jpg", [])
            : () => storage.InsertAsync("test.jpg", []);

        var exception = await Assert.ThrowsAsync<ArgumentException>(operation);

        Assert.Equal("imageBytes", exception.ParamName);
        Assert.Empty(Directory.GetFiles(insertOriginal ? OriginalDirectory : PrimaryDirectory));
    }

    [Fact]
    public async Task GetInfoAsync_ExistingFile_ReturnsImageInfo()
    {
        Directory.CreateDirectory(_tempDirectory);
        var bytes = Encoding.UTF8.GetBytes("image data");
        var filePath = Path.Combine(PrimaryDirectory, "test.png");
        Directory.CreateDirectory(PrimaryDirectory);
        await File.WriteAllBytesAsync(filePath, bytes, TestContext.Current.CancellationToken);
        var storage = CreateStorage();

        var result = await storage.GetInfoAsync("test.png");

        Assert.NotNull(result);
        Assert.Equal("png", result.ImageExtensionName);
        Assert.Equal("image/png", result.ImageContentType);
        Assert.Equal(bytes.Length, result.ContentLength);
        Assert.NotNull(result.LastModifiedUtc);
        Assert.NotNull(result.EntityTag);
    }

    [Fact]
    public async Task GetInfoAsync_MissingFile_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();

        var result = await storage.GetInfoAsync("missing.jpg");

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenReadAsync_ExistingFile_ReturnsReadableStream()
    {
        Directory.CreateDirectory(_tempDirectory);
        var bytes = Encoding.UTF8.GetBytes("image data");
        Directory.CreateDirectory(PrimaryDirectory);
        await File.WriteAllBytesAsync(Path.Combine(PrimaryDirectory, "test.png"), bytes, TestContext.Current.CancellationToken);
        var storage = CreateStorage();

        await using var result = await storage.OpenReadAsync("test.png");

        Assert.NotNull(result);
        using var reader = new MemoryStream();
        await result.CopyToAsync(reader, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, reader.ToArray());
    }

    [Fact]
    public async Task OpenReadAsync_MissingFile_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();

        var result = await storage.OpenReadAsync("missing.jpg");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_RemovesFile()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(PrimaryDirectory, "test.jpg");
        Directory.CreateDirectory(PrimaryDirectory);
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

        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetInfoAsync(fileName));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(fileName));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.InsertAsync(fileName, [1, 2, 3]));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.InsertOriginalAsync(fileName, [1, 2, 3]));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(fileName));
    }

    [Fact]
    public async Task InsertOriginalAsync_WritesOnlyToOriginalPath()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        var bytes = Encoding.UTF8.GetBytes("original");

        var result = await storage.InsertOriginalAsync("photo-origin.jpg", bytes);

        Assert.Equal("photo-origin.jpg", result);
        Assert.False(File.Exists(Path.Combine(PrimaryDirectory, "photo-origin.jpg")));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(OriginalDirectory, "photo-origin.jpg"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PrimaryOperations_DoNotReadOrDeleteOriginalImage()
    {
        Directory.CreateDirectory(_tempDirectory);
        var storage = CreateStorage();
        var bytes = Encoding.UTF8.GetBytes("original");
        var originalFilePath = Path.Combine(OriginalDirectory, "photo-origin.jpg");
        await storage.InsertOriginalAsync("photo-origin.jpg", bytes);

        var info = await storage.GetInfoAsync("photo-origin.jpg");
        var stream = await storage.OpenReadAsync("photo-origin.jpg");
        await storage.DeleteAsync("photo-origin.jpg");

        Assert.Null(info);
        Assert.Null(stream);
        Assert.True(File.Exists(originalFilePath));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(originalFilePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_WithSamePrimaryAndOriginalPath_ThrowsInvalidOperationException()
    {
        Directory.CreateDirectory(_tempDirectory);
        var configuration = new FileSystemImageConfiguration(PrimaryDirectory, PrimaryDirectory);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new FileSystemImageStorage(configuration, Mock.Of<ILogger<FileSystemImageStorage>>()));

        Assert.Contains("must not overlap", exception.Message);
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

    [Fact]
    public void ResolveImageStoragePaths_DistinctPaths_CreatesBothDirectories()
    {
        var primaryPath = Path.Combine(_tempDirectory, "primary");
        var originalPath = Path.Combine(_tempDirectory, "original");

        var result = FileSystemImageStorage.ResolveImageStoragePaths(primaryPath, originalPath);

        Assert.Equal(Path.GetFullPath(primaryPath), result.PrimaryPath);
        Assert.Equal(Path.GetFullPath(originalPath), result.OriginalPath);
        Assert.True(Directory.Exists(primaryPath));
        Assert.True(Directory.Exists(originalPath));
    }

    [Fact]
    public void ResolveImageStoragePaths_SameResolvedPath_ThrowsInvalidOperationException()
    {
        var primaryPath = Path.Combine(_tempDirectory, "images");
        var equivalentOriginalPath = Path.Combine(primaryPath, ".");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            FileSystemImageStorage.ResolveImageStoragePaths(primaryPath, equivalentOriginalPath));

        Assert.Contains("must not overlap", exception.Message);
    }

    [Fact]
    public void ResolveImageStoragePaths_NestedPaths_ThrowsInvalidOperationException()
    {
        var primaryPath = Path.Combine(_tempDirectory, "images");
        var originalPath = Path.Combine(primaryPath, "original");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            FileSystemImageStorage.ResolveImageStoragePaths(primaryPath, originalPath));

        Assert.Contains("must not overlap", exception.Message);
    }

    [Fact]
    public void DefaultPaths_AreDistinctSiblingDirectories()
    {
        Assert.NotEqual(FileSystemImageStorage.DefaultPath, FileSystemImageStorage.DefaultOriginalPath);
        Assert.Equal("images", Path.GetFileName(FileSystemImageStorage.DefaultPath));
        Assert.Equal("images-origin", Path.GetFileName(FileSystemImageStorage.DefaultOriginalPath));
        Assert.Equal(
            Path.GetDirectoryName(FileSystemImageStorage.DefaultPath),
            Path.GetDirectoryName(FileSystemImageStorage.DefaultOriginalPath));
    }

    private FileSystemImageStorage CreateStorage()
    {
        Directory.CreateDirectory(PrimaryDirectory);
        Directory.CreateDirectory(OriginalDirectory);
        return new FileSystemImageStorage(
            new FileSystemImageConfiguration(PrimaryDirectory, OriginalDirectory),
            Mock.Of<ILogger<FileSystemImageStorage>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
