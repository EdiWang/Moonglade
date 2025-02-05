namespace Moonglade.ImageStorage.Providers;

public class AzureBlobConfiguration(string connectionString, string containerName, string secondaryContainerName = null)
{
    public string ConnectionString { get; } = connectionString;

    public string ContainerName { get; } = containerName;

    public string SecondaryContainerName { get; } = secondaryContainerName;
}