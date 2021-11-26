namespace Moonglade.ImageStorage.Providers;

public class AzureBlobConfiguration
{
    public string ConnectionString { get; }

    public string ContainerName { get; }

    public AzureBlobConfiguration(string connectionString, string containerName)
    {
        ConnectionString = connectionString;
        ContainerName = containerName;
    }
}