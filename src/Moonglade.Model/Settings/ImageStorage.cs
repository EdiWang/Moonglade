namespace Moonglade.Model.Settings
{
    public class ImageStorage
    {
        public string Provider { get; set; }

        public AzureStorageSettings AzureStorageSettings { get; set; }

        public FileSystemSettings FileSystemSettings { get; set; }
    }
}
