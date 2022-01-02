using Pzy.Qiniu;

namespace Moonglade.ImageStorage.Providers
{
    public class QiniuBlobConfiguration : DefaultQiniuConfiguration
    {
        private readonly bool _withSSL;
        private readonly string _endPoint;
        private readonly string _bucketName;

        public QiniuBlobConfiguration(string endPoint, string bucketName, bool withSSL)
        {
            _endPoint = endPoint;
            _bucketName = bucketName;
            _withSSL = withSSL;
        }

        public override Zone Zone => Zone.ZONE_CN_South;

        public override bool UseHttps => _withSSL;

        public override bool UseCdnDomains => true;

        public override ChunkUnit ChunkSize => ChunkUnit.U512K;

        public override string EndPoint => _endPoint;

        public override string BucketName => _bucketName;
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
}
