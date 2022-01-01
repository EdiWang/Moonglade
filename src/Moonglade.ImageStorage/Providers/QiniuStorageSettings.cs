namespace Moonglade.ImageStorage.Providers
{
    public class QiniuStorageSettings
    {
        public string EndPoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public bool WithSSL { get; set; }
    }
}
