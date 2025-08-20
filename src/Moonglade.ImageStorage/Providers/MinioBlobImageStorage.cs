using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Moonglade.ImageStorage.Providers;

/// <summary>
/// Configuration record for MinIO blob storage settings.
/// </summary>
/// <param name="EndPoint">The MinIO server endpoint URL.</param>
/// <param name="AccessKey">The access key for MinIO authentication.</param>
/// <param name="SecretKey">The secret key for MinIO authentication.</param>
/// <param name="BucketName">The primary bucket name for storing images.</param>
/// <param name="SecondaryBucketName">The optional secondary bucket name for additional storage.</param>
/// <param name="WithSSL">Indicates whether to use SSL/TLS for connections.</param>
public record MinioBlobConfiguration(
    string EndPoint,
    string AccessKey,
    string SecretKey,
    string BucketName,
    string SecondaryBucketName = null,
    bool WithSSL = false);

/// <summary>
/// Provides MinIO blob storage implementation for blog image storage operations.
/// </summary>
public class MinioBlobImageStorage : IBlogImageStorage
{
    /// <summary>
    /// Gets the name of this storage provider.
    /// </summary>
    public string Name => nameof(MinioBlobImageStorage);

    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly string _secondaryBucketName;

    private readonly ILogger<MinioBlobImageStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinioBlobImageStorage"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="blobConfiguration">The MinIO configuration settings.</param>
    public MinioBlobImageStorage(ILogger<MinioBlobImageStorage> logger, MinioBlobConfiguration blobConfiguration)
    {
        _logger = logger;

        _client = new MinioClient()
            .WithEndpoint(blobConfiguration.EndPoint)
            .WithCredentials(blobConfiguration.AccessKey, blobConfiguration.SecretKey);
        if (blobConfiguration.WithSSL)
        {
            _client = _client.WithSSL();
        }
        _client.Build();

        _bucketName = blobConfiguration.BucketName;
        _secondaryBucketName = blobConfiguration.SecondaryBucketName;

        logger.LogInformation("Created {StorageName} at {EndPoint}", nameof(MinioBlobImageStorage), blobConfiguration.EndPoint);
    }

    /// <summary>
    /// Creates the primary bucket if it doesn't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task CreateBucketIfNotExists()
    {
        var arg = new BucketExistsArgs().WithBucket(_bucketName);
        if (!await _client.BucketExistsAsync(arg))
        {
            var arg1 = new MakeBucketArgs().WithBucket(_bucketName);
            await _client.MakeBucketAsync(arg1);
        }
    }

    /// <summary>
    /// Inserts an image into the primary bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to insert.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file name.</returns>
    public Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        return InsertInternalAsync(fileName, imageBytes, _bucketName);
    }

    /// <summary>
    /// Inserts an image into the secondary bucket if configured.
    /// </summary>
    /// <param name="fileName">The name of the file to insert.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file name, or null if secondary bucket is not configured.</returns>
    public async Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(_secondaryBucketName))
        {
            _logger.LogError("Secondary bucket is not configured.");
            return null;
        }

        return await InsertInternalAsync(fileName, imageBytes, _secondaryBucketName);
    }

    /// <summary>
    /// Internal method to insert an image into a specified bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to insert.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <param name="bucketName">The target bucket name.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileName is null or whitespace.</exception>
    public async Task<string> InsertInternalAsync(string fileName, byte[] imageBytes, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _logger.LogInformation("Uploading {FileName} to Minio Blob Storage.", fileName);

        await CreateBucketIfNotExists();

        await using var fileStream = new MemoryStream(imageBytes);

        var putObjectArg = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length);

        await _client.PutObjectAsync(putObjectArg);

        _logger.LogInformation("Uploaded image file '{FileName}' to Minio Blob Storage.", fileName);

        return fileName;
    }

    /// <summary>
    /// Deletes an image from the primary bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAsync(string fileName)
    {
        if (await BlobExistsAsync(fileName))
        {
            await _client.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName));
        }
    }

    /// <summary>
    /// Checks if a blob exists in the primary bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the blob exists; otherwise, false.</returns>
    private async Task<bool> BlobExistsAsync(string fileName)
    {
        // Make sure Blob Container exists.
        if (!await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName))) return false;

        try
        {
            var arg = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _client.StatObjectAsync(arg);
        }
        catch (Exception e)
        {
            if (e is ObjectNotFoundException)
            {
                return false;
            }
            throw;
        }
        return true;
    }

    /// <summary>
    /// Retrieves an image from the primary bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the image information, or null if the file doesn't exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the file extension is empty.</exception>
    public async Task<ImageInfo> GetAsync(string fileName)
    {
        await using var memoryStream = new MemoryStream();
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("File extension is empty");
        }

        var exists = await BlobExistsAsync(fileName);
        if (!exists)
        {
            _logger.LogWarning("Blob {FileName} does not exist.", fileName);
            return null;
        }

        var arg = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream =>
        {
            stream?.CopyTo(memoryStream);
        });

        await _client.GetObjectAsync(arg);
        var arr = memoryStream.ToArray();

        var fileType = extension.Replace(".", string.Empty);
        var imageInfo = new ImageInfo
        {
            ImageBytes = arr,
            ImageExtensionName = fileType
        };

        return imageInfo;
    }
}