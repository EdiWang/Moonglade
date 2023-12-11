namespace Moonglade.ImageStorage.Providers;

public class AzureBlobConfiguration(string connectionString, string containerName)
{
    public string ConnectionString { get; } = connectionString;

    public string ContainerName { get; } = containerName;
}