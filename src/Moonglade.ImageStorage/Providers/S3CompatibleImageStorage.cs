using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Moonglade.ImageStorage.Providers;

public record S3CompatibleStorageConfiguration(
    string ServiceUrl,
    string Region,
    string AccessKeyId,
    string SecretAccessKey,
    string BucketName,
    string SecondaryBucketName = null,
    bool ForcePathStyle = false);

public class S3CompatibleImageStorage(
    ILogger<S3CompatibleImageStorage> logger,
    S3CompatibleStorageConfiguration storageConfiguration,
    IAmazonS3 s3Client) : IBlogImageStorage
{
    private const string DefaultAuthenticationRegion = "us-east-1";

    public string Name => nameof(S3CompatibleImageStorage);

    public static IAmazonS3 CreateClient(S3CompatibleStorageConfiguration storageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(storageConfiguration);

        var clientConfig = new AmazonS3Config
        {
            ServiceURL = storageConfiguration.ServiceUrl.TrimEnd('/'),
            ForcePathStyle = storageConfiguration.ForcePathStyle,
            AuthenticationRegion = string.IsNullOrWhiteSpace(storageConfiguration.Region)
                ? DefaultAuthenticationRegion
                : storageConfiguration.Region
        };

        var credentials = new BasicAWSCredentials(
            storageConfiguration.AccessKeyId,
            storageConfiguration.SecretAccessKey);

        return new AmazonS3Client(credentials, clientConfig);
    }

    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        return await InsertInternalAsync(storageConfiguration.BucketName, fileName, imageBytes).ConfigureAwait(false);
    }

    public async Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(storageConfiguration.SecondaryBucketName))
        {
            logger.LogError("Secondary bucket is not configured.");
            throw new InvalidOperationException("Secondary bucket is not configured.");
        }

        return await InsertInternalAsync(storageConfiguration.SecondaryBucketName, fileName, imageBytes).ConfigureAwait(false);
    }

    public async Task<ImageInfo> GetInfoAsync(string fileName)
    {
        var extension = ValidateImageFileName(fileName);

        try
        {
            var response = await s3Client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = storageConfiguration.BucketName,
                    Key = fileName
                }).ConfigureAwait(false);

            logger.LogInformation("Fetched object metadata for '{FileName}' from S3-compatible storage.", fileName);

            return new ImageInfo
            {
                ImageExtensionName = extension.TrimStart('.'),
                ContentType = string.IsNullOrWhiteSpace(response.ContentType)
                    ? ImageInfo.GetContentType(extension)
                    : response.ContentType,
                ContentLength = response.Headers.ContentLength,
                LastModifiedUtc = response.LastModified,
                EntityTag = response.ETag
            };
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            logger.LogWarning("Object '{FileName}' does not exist.", fileName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch object metadata for '{FileName}' from S3-compatible storage.", fileName);
            throw;
        }
    }

    public async Task<Stream> OpenReadAsync(string fileName)
    {
        ValidateImageFileName(fileName);

        try
        {
            var response = await s3Client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = storageConfiguration.BucketName,
                    Key = fileName
                }).ConfigureAwait(false);

            return new S3ObjectReadStream(response);
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            logger.LogWarning("Object '{FileName}' does not exist.", fileName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open object stream for '{FileName}' from S3-compatible storage.", fileName);
            throw;
        }
    }

    public async Task DeleteAsync(string fileName)
    {
        ValidateImageFileName(fileName);

        try
        {
            logger.LogInformation("Deleting object '{FileName}' from S3-compatible storage.", fileName);
            await s3Client.DeleteObjectAsync(
                new DeleteObjectRequest
                {
                    BucketName = storageConfiguration.BucketName,
                    Key = fileName
                }).ConfigureAwait(false);

            logger.LogInformation("Delete request completed for object '{FileName}' in S3-compatible storage.", fileName);
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            logger.LogWarning("Object '{FileName}' did not exist during deletion attempt.", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete object '{FileName}' from S3-compatible storage.", fileName);
            throw;
        }
    }

    private async Task<string> InsertInternalAsync(string bucketName, string fileName, byte[] imageBytes)
    {
        var extension = ValidateImageFileName(fileName);
        ArgumentNullException.ThrowIfNull(imageBytes);

        if (imageBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));
        }

        logger.LogInformation("Uploading '{FileName}' to S3-compatible storage bucket '{BucketName}'.", fileName, bucketName);

        try
        {
            await using var fileStream = new MemoryStream(imageBytes);
            var response = await s3Client.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = fileStream,
                    ContentType = ImageInfo.GetContentType(extension),
                    AutoCloseStream = false
                }).ConfigureAwait(false);

            logger.LogInformation(
                "Uploaded '{FileName}' to S3-compatible storage bucket '{BucketName}'. ETag: '{ETag}'.",
                fileName,
                bucketName,
                response.ETag);

            return fileName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload '{FileName}' to S3-compatible storage bucket '{BucketName}'.", fileName, bucketName);
            throw;
        }
    }

    private static string ValidateImageFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var sanitizedFileName = Path.GetFileName(fileName);
        if (!string.Equals(sanitizedFileName, fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("File name contains invalid path characters.", nameof(fileName));
        }

        if (fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("File name contains path traversal sequences.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("File extension is empty.", nameof(fileName));
        }

        return extension;
    }

    private static bool IsNotFound(AmazonS3Exception exception)
    {
        return exception.StatusCode == HttpStatusCode.NotFound ||
               string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class S3ObjectReadStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _stream = response.ResponseStream;

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
