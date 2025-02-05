namespace Moonglade.ImageStorage.Providers;

public class MinioBlobConfiguration(string endPoint, string accessKey, string secretKey, string bucketName, string secondaryBucketName = null, bool withSSL = false)
{
    public string EndPoint { get; } = endPoint;
    public string AccessKey { get; } = accessKey;
    public string SecretKey { get; } = secretKey;
    public string BucketName { get; } = bucketName;
    public string SecondaryBucketName { get; } = secondaryBucketName;
    public bool WithSSL { get; } = withSSL;
}