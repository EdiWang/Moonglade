using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Moonglade.ImageStorage.Providers;
using Moq;
using System.Text;

namespace Moonglade.ImageStorage.Tests.Providers;

public class AzureBlobImageStorageTests
{
    private readonly Mock<ILogger<AzureBlobImageStorage>> _mockLogger;
    private readonly Mock<BlobContainerClient> _mockContainer;
    private readonly Mock<BlobContainerClient> _mockSecondaryContainer;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly AzureBlobConfiguration _configuration;
    private readonly AzureBlobConfiguration _configurationWithSecondary;

    public AzureBlobImageStorageTests()
    {
        _mockLogger = new Mock<ILogger<AzureBlobImageStorage>>();
        _mockContainer = new Mock<BlobContainerClient>();
        _mockSecondaryContainer = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();

        _configuration = new AzureBlobConfiguration(
            "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=dGVzdGtleQ==;EndpointSuffix=core.windows.net",
            "primary-container"
        );

        _configurationWithSecondary = new AzureBlobConfiguration(
            "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=dGVzdGtleQ==;EndpointSuffix=core.windows.net",
            "primary-container",
            "secondary-container"
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Act
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Assert
        Assert.NotNull(storage);
        Assert.Equal(nameof(AzureBlobImageStorage), storage.Name);
    }

    [Fact]
    public void Constructor_WithSecondaryContainer_InitializesSuccessfully()
    {
        // Act
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configurationWithSecondary);

        // Assert
        Assert.NotNull(storage);
        Assert.Equal(nameof(AzureBlobImageStorage), storage.Name);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureBlobImageStorage(null!, _configuration));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsNullReferenceException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => 
            new AzureBlobImageStorage(_mockLogger.Object, null!));
    }

    #endregion

    #region Name Property Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Act & Assert
        Assert.Equal(nameof(AzureBlobImageStorage), storage.Name);
    }

    #endregion

    #region InsertAsync Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task InsertAsync_WithInvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);
        var imageBytes = Encoding.UTF8.GetBytes("fake image data");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.InsertAsync(fileName!, imageBytes));
    }

    [Fact]
    public async Task InsertAsync_WithNullImageBytes_ThrowsArgumentNullException()
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);
        const string fileName = "test.jpg";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            storage.InsertAsync(fileName, null!));
    }

    [Fact]
    public async Task InsertAsync_WithEmptyImageBytes_ThrowsArgumentException()
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);
        const string fileName = "test.jpg";
        var emptyImageBytes = Array.Empty<byte>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.InsertAsync(fileName, emptyImageBytes));
        Assert.Equal("imageBytes", exception.ParamName);
        Assert.Contains("Image bytes cannot be empty", exception.Message);
    }

    #endregion

    #region InsertSecondaryAsync Tests

    [Fact]
    public async Task InsertSecondaryAsync_WithoutSecondaryContainer_ThrowsInvalidOperationException()
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);
        const string fileName = "test.jpg";
        var imageBytes = Encoding.UTF8.GetBytes("fake image data");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            storage.InsertSecondaryAsync(fileName, imageBytes));
        Assert.Contains("Secondary container is not configured", exception.Message);

        VerifySecondaryContainerErrorLogging();
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        const string fileName = "nonexistent.jpg";

        var storage = CreateStorageWithMockedContainer();
        _mockBlobClient.Setup(x => x.ExistsAsync(default))
                      .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        // Act
        var result = await storage.GetAsync(fileName);

        // Assert
        Assert.Null(result);
        VerifyNonExistentFileLogging(fileName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task GetAsync_WithInvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.GetAsync(fileName!));
    }

    [Theory]
    [InlineData("filename")]
    [InlineData("noextension")]
    [InlineData("file.")]
    public async Task GetAsync_WithoutExtension_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.GetAsync(fileName));
        Assert.Equal("fileName", exception.ParamName);
        Assert.Contains("File extension is empty", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingFile_DeletesSuccessfully()
    {
        // Arrange
        const string fileName = "test.jpg";

        var storage = CreateStorageWithMockedContainer();
        _mockContainer.Setup(x => x.DeleteBlobIfExistsAsync(fileName, DeleteSnapshotsOption.None, null, default))
                     .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Act
        await storage.DeleteAsync(fileName);

        // Assert
        VerifyDeleteCalls(fileName);
        VerifySuccessfulDeleteLogging(fileName);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentFile_LogsWarning()
    {
        // Arrange
        const string fileName = "nonexistent.jpg";

        var storage = CreateStorageWithMockedContainer();
        _mockContainer.Setup(x => x.DeleteBlobIfExistsAsync(fileName, DeleteSnapshotsOption.None, null, default))
                     .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        // Act
        await storage.DeleteAsync(fileName);

        // Assert
        VerifyDeleteCalls(fileName);
        VerifyNonExistentDeleteLogging(fileName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task DeleteAsync_WithInvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.DeleteAsync(fileName!));
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleteFails_ThrowsAndLogsError()
    {
        // Arrange
        const string fileName = "test.jpg";
        var expectedException = new RequestFailedException("Delete failed");

        var storage = CreateStorageWithMockedContainer();
        _mockContainer.Setup(x => x.DeleteBlobIfExistsAsync(fileName, DeleteSnapshotsOption.None, null, default))
                     .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() => 
            storage.DeleteAsync(fileName));
        Assert.Equal(expectedException, exception);

        VerifyDeleteErrorLogging(fileName);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void AzureBlobImageStorage_ImplementsIBlogImageStorage()
    {
        // Arrange & Act
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Assert
        Assert.IsAssignableFrom<IBlogImageStorage>(storage);
    }

    [Fact]
    public void IBlogImageStorage_NameProperty_ReturnsExpectedValue()
    {
        // Arrange
        IBlogImageStorage storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);

        // Act & Assert
        Assert.Equal(nameof(AzureBlobImageStorage), storage.Name);
    }

    #endregion

    #region Helper Methods

    private AzureBlobImageStorage CreateStorageWithMockedContainer()
    {
        _mockContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                     .Returns(_mockBlobClient.Object);

        // Use reflection to create storage with mocked container
        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configuration);
        
        // Replace the private _container field with our mock
        var containerField = typeof(AzureBlobImageStorage).GetField("_container", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        containerField?.SetValue(storage, _mockContainer.Object);

        return storage;
    }

    private AzureBlobImageStorage CreateStorageWithSecondaryContainer()
    {
        _mockContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                     .Returns(_mockBlobClient.Object);
        _mockSecondaryContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                              .Returns(_mockBlobClient.Object);

        var storage = new AzureBlobImageStorage(_mockLogger.Object, _configurationWithSecondary);
        
        // Replace the private fields with our mocks
        var containerField = typeof(AzureBlobImageStorage).GetField("_container", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var secondaryContainerField = typeof(AzureBlobImageStorage).GetField("_secondaryContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        containerField?.SetValue(storage, _mockContainer.Object);
        secondaryContainerField?.SetValue(storage, _mockSecondaryContainer.Object);

        return storage;
    }

    private void SetupSuccessfulDownload(string fileName, byte[] imageBytes)
    {
        _mockBlobClient.Setup(x => x.ExistsAsync(default))
                      .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        
        _mockBlobClient.Setup(x => x.DownloadToAsync(It.IsAny<MemoryStream>(), default))
                      .Callback<Stream, CancellationToken>((stream, _) => 
                      {
                          stream.Write(imageBytes, 0, imageBytes.Length);
                      })
                      .Returns(Task.FromResult(Mock.Of<Response>()));
    }

    private void VerifyDownloadCalls(string fileName)
    {
        _mockContainer.Verify(x => x.GetBlobClient(fileName), Times.Once);
        _mockBlobClient.Verify(x => x.ExistsAsync(default), Times.Once);
        _mockBlobClient.Verify(x => x.DownloadToAsync(It.IsAny<MemoryStream>(), default), Times.Once);
    }

    private void VerifyDeleteCalls(string fileName)
    {
        _mockContainer.Verify(x => x.DeleteBlobIfExistsAsync(fileName, DeleteSnapshotsOption.None, null, default), Times.Once);
    }

    private void VerifyErrorLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to upload '{fileName}'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifySecondaryContainerErrorLogging()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secondary container is not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyNonExistentFileLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Blob '{fileName}' does not exist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyDownloadErrorLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to fetch blob '{fileName}'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifySuccessfulDeleteLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully deleted blob '{fileName}'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyNonExistentDeleteLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Blob '{fileName}' did not exist during deletion attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyDeleteErrorLogging(string fileName)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to delete blob '{fileName}'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}