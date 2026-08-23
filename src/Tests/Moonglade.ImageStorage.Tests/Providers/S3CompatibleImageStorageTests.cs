using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Moonglade.ImageStorage.Providers;
using Moq;
using System.Net;
using System.Text;

namespace Moonglade.ImageStorage.Tests.Providers;

public class S3CompatibleImageStorageTests
{
    private readonly Mock<ILogger<S3CompatibleImageStorage>> _logger = new();
    private readonly Mock<IAmazonS3> _s3Client = new();
    private readonly S3CompatibleStorageConfiguration _configuration = new(
        "https://storage.example.com",
        "us-east-1",
        "access-key",
        "secret-key",
        "primary-bucket",
        "secondary-bucket",
        true);

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        var storage = CreateStorage();

        Assert.Equal(nameof(S3CompatibleImageStorage), storage.Name);
    }

    [Fact]
    public async Task InsertAsync_WithValidImage_UploadsToPrimaryBucketAndReturnsFileName()
    {
        var bytes = Encoding.UTF8.GetBytes("image data");
        PutObjectRequest capturedRequest = null;
        byte[] capturedBytes = null;
        _s3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
                using var reader = new MemoryStream();
                request.InputStream.CopyTo(reader);
                capturedBytes = reader.ToArray();
            })
            .ReturnsAsync(new PutObjectResponse { ETag = "\"etag\"" });
        var storage = CreateStorage();

        var result = await storage.InsertAsync("photo.png", bytes);

        Assert.Equal("photo.png", result);
        Assert.NotNull(capturedRequest);
        Assert.Equal("primary-bucket", capturedRequest.BucketName);
        Assert.Equal("photo.png", capturedRequest.Key);
        Assert.Equal("image/png", capturedRequest.ContentType);
        Assert.True(capturedRequest.AutoResetStreamPosition);
        Assert.Equal(bytes, capturedBytes);
    }

    [Fact]
    public async Task InsertOriginalAsync_WithConfiguredBucket_UploadsToSecondaryBucket()
    {
        var bytes = Encoding.UTF8.GetBytes("image data");
        PutObjectRequest capturedRequest = null;
        _s3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutObjectResponse());
        var storage = CreateStorage();

        var result = await storage.InsertOriginalAsync("photo-origin.jpg", bytes);

        Assert.Equal("photo-origin.jpg", result);
        Assert.NotNull(capturedRequest);
        Assert.Equal("secondary-bucket", capturedRequest.BucketName);
        Assert.Equal("photo-origin.jpg", capturedRequest.Key);
        Assert.Equal("image/jpeg", capturedRequest.ContentType);
    }

    [Fact]
    public async Task InsertOriginalAsync_WithoutSecondaryBucket_ThrowsInvalidOperationException()
    {
        var configuration = new S3CompatibleStorageConfiguration(
            _configuration.ServiceUrl,
            _configuration.Region,
            _configuration.AccessKeyId,
            _configuration.SecretAccessKey,
            _configuration.BucketName);
        var storage = new S3CompatibleImageStorage(_logger.Object, configuration, _s3Client.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.InsertOriginalAsync("photo-origin.jpg", [1, 2, 3]));

        Assert.Contains("Secondary bucket is not configured", exception.Message);
        _s3Client.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../test.jpg")]
    [InlineData("folder/test.jpg")]
    [InlineData("test..jpg")]
    [InlineData("filename")]
    public async Task Operations_WithInvalidFileName_ThrowArgumentException(string fileName)
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetInfoAsync(fileName));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(fileName));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.InsertAsync(fileName, [1, 2, 3]));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(fileName));
    }

    [Fact]
    public async Task InsertAsync_WithNullImageBytes_ThrowsArgumentNullException()
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<ArgumentNullException>(() => storage.InsertAsync("photo.jpg", null));
    }

    [Fact]
    public async Task InsertAsync_WithEmptyImageBytes_ThrowsArgumentException()
    {
        var storage = CreateStorage();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => storage.InsertAsync("photo.jpg", []));

        Assert.Equal("imageBytes", exception.ParamName);
    }

    [Fact]
    public async Task GetInfoAsync_WhenObjectExists_ReturnsImageInfo()
    {
        var lastModified = new DateTime(2026, 8, 4, 10, 30, 0, DateTimeKind.Utc);
        var response = new GetObjectMetadataResponse
        {
            ContentType = "image/webp",
            LastModified = lastModified,
            ETag = "\"etag-value\""
        };
        response.Headers.ContentLength = 123;
        _s3Client
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var storage = CreateStorage();

        var result = await storage.GetInfoAsync("photo.webp");

        Assert.NotNull(result);
        Assert.Equal("webp", result.ImageExtensionName);
        Assert.Equal("image/webp", result.ImageContentType);
        Assert.Equal(123, result.ContentLength);
        Assert.Equal(lastModified, result.LastModifiedUtc);
        Assert.Equal("\"etag-value\"", result.EntityTag);

        _s3Client.Verify(x => x.GetObjectMetadataAsync(
            It.Is<GetObjectMetadataRequest>(request =>
                request.BucketName == "primary-bucket" &&
                request.Key == "photo.webp"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInfoAsync_WhenObjectContentTypeIsMissing_UsesExtensionContentType()
    {
        var response = new GetObjectMetadataResponse();
        response.Headers.ContentLength = 10;
        _s3Client
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var storage = CreateStorage();

        var result = await storage.GetInfoAsync("photo.svg");

        Assert.NotNull(result);
        Assert.Equal("image/svg+xml", result.ImageContentType);
    }

    [Fact]
    public async Task GetInfoAsync_WhenObjectDoesNotExist_ReturnsNull()
    {
        _s3Client
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateNotFoundException());
        var storage = CreateStorage();

        var result = await storage.GetInfoAsync("missing.png");

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenReadAsync_WhenObjectExists_ReturnsReadableStream()
    {
        var bytes = Encoding.UTF8.GetBytes("image data");
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream(bytes)
        };
        _s3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var storage = CreateStorage();

        await using var result = await storage.OpenReadAsync("photo.png");

        Assert.NotNull(result);
        using var reader = new MemoryStream();
        await result.CopyToAsync(reader, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, reader.ToArray());

        _s3Client.Verify(x => x.GetObjectAsync(
            It.Is<GetObjectRequest>(request =>
                request.BucketName == "primary-bucket" &&
                request.Key == "photo.png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenReadAsync_WhenObjectDoesNotExist_ReturnsNull()
    {
        _s3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateNotFoundException());
        var storage = CreateStorage();

        var result = await storage.OpenReadAsync("missing.png");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithValidFileName_DeletesPrimaryObject()
    {
        _s3Client
            .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());
        var storage = CreateStorage();

        await storage.DeleteAsync("photo.jpg");

        _s3Client.Verify(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(request =>
                request.BucketName == "primary-bucket" &&
                request.Key == "photo.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectDoesNotExist_DoesNotThrow()
    {
        _s3Client
            .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateNotFoundException());
        var storage = CreateStorage();

        await storage.DeleteAsync("missing.jpg");
    }

    [Fact]
    public void CreateClient_ConfiguresServiceUrlRegionAndPathStyle()
    {
        using var client = S3CompatibleImageStorage.CreateClient(_configuration);
        var amazonClient = Assert.IsType<AmazonS3Client>(client);
        var clientConfig = Assert.IsType<AmazonS3Config>(amazonClient.Config);

        Assert.Equal("https://storage.example.com/", clientConfig.ServiceURL);
        Assert.Equal("us-east-1", clientConfig.AuthenticationRegion);
        Assert.True(clientConfig.ForcePathStyle);
    }

    [Fact]
    public void CreateClient_WithEmptyRegion_UsesDefaultAuthenticationRegion()
    {
        var configuration = new S3CompatibleStorageConfiguration(
            _configuration.ServiceUrl,
            string.Empty,
            _configuration.AccessKeyId,
            _configuration.SecretAccessKey,
            _configuration.BucketName,
            _configuration.SecondaryBucketName,
            _configuration.ForcePathStyle);

        using var client = S3CompatibleImageStorage.CreateClient(configuration);
        var amazonClient = Assert.IsType<AmazonS3Client>(client);
        var clientConfig = Assert.IsType<AmazonS3Config>(amazonClient.Config);

        Assert.Equal("us-east-1", clientConfig.AuthenticationRegion);
    }

    private S3CompatibleImageStorage CreateStorage()
    {
        return new S3CompatibleImageStorage(_logger.Object, _configuration, _s3Client.Object);
    }

    private static AmazonS3Exception CreateNotFoundException()
    {
        return new AmazonS3Exception(
            "Object not found.",
            ErrorType.Sender,
            "NoSuchKey",
            "request-id",
            HttpStatusCode.NotFound);
    }
}
