using Microsoft.Extensions.Logging;

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
    S3CompatibleStorageConfiguration storageConfiguration) : IBlogImageStorage
{
    public string Name => nameof(S3CompatibleImageStorage);

    public Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        LogNotImplemented();
        throw new NotImplementedException("S3-compatible image storage is registered but storage operations are not implemented yet.");
    }

    public Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes)
    {
        LogNotImplemented();
        throw new NotImplementedException("S3-compatible image storage is registered but storage operations are not implemented yet.");
    }

    public Task<ImageInfo> GetInfoAsync(string fileName)
    {
        LogNotImplemented();
        throw new NotImplementedException("S3-compatible image storage is registered but storage operations are not implemented yet.");
    }

    public Task<Stream> OpenReadAsync(string fileName)
    {
        LogNotImplemented();
        throw new NotImplementedException("S3-compatible image storage is registered but storage operations are not implemented yet.");
    }

    public Task DeleteAsync(string fileName)
    {
        LogNotImplemented();
        throw new NotImplementedException("S3-compatible image storage is registered but storage operations are not implemented yet.");
    }

    private void LogNotImplemented()
    {
        logger.LogError(
            "S3-compatible image storage operations are not implemented yet. ServiceUrl: {ServiceUrl}, BucketName: {BucketName}, ForcePathStyle: {ForcePathStyle}",
            storageConfiguration.ServiceUrl,
            storageConfiguration.BucketName,
            storageConfiguration.ForcePathStyle);
    }
}
