using Pzy.Qiniu;

namespace Moonglade.ImageStorage.Providers;

public class QiniuBlobConfiguration : DefaultQiniuConfiguration
{
    public QiniuBlobConfiguration(string endPoint, string bucketName, bool withSSL)
    {
        EndPoint = endPoint;
        BucketName = bucketName;
        UseHttps = withSSL;
    }

    public override Zone Zone => Zone.ZONE_CN_South;

    public override bool UseHttps { get; }

    public override bool UseCdnDomains => true;

    public override ChunkUnit ChunkSize => ChunkUnit.U512K;

    public override string EndPoint { get; }

    public override string BucketName { get; }
}

public class MacSettings : IMacSettings
{
    public MacSettings(string accessKey, string secretKey)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
    }

    public string AccessKey { get; }

    public string SecretKey { get; }
}