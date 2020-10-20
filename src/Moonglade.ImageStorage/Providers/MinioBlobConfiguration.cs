namespace Moonglade.ImageStorage.Providers
{
    public class MinioBlobConfiguration
    {
        public string EndPoint { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }
        public string BucketName { get; }
        public bool WithSSL { get; }

        public MinioBlobConfiguration(string endPoint, string accessKey, string secretKey, string bucketName, bool withSSL)
        {
            EndPoint = endPoint;
            AccessKey = accessKey;
            SecretKey = secretKey;
            BucketName = bucketName;
            WithSSL = withSSL;
        }
    }
}
