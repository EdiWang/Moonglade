namespace Moonglade.ImageStorage.AzureBlob
{
    public class AzureStorageInfo
    {
        public string ConnectionString { get; }

        public string ContainerName { get; }

        public AzureStorageInfo(string connectionString, string containerName)
        {
            ConnectionString = connectionString;
            ContainerName = containerName;
        }
    }
}
