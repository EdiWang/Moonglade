namespace Moonglade.ImageStorage
{
    public class ImageStorageSettings
    {
        public string Provider { get; set; }

        public AzureStorageSettings AzureStorageSettings { get; set; }

        public FileSystemSettings FileSystemSettings { get; set; }

        public CDNSettings CDNSettings { get; set; }
    }
}
