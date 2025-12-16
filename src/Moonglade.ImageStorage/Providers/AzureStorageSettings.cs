namespace Moonglade.ImageStorage.Providers;

public record AzureStorageSettings
{
    public string ConnectionString { get; set; }

    public string ContainerName { get; set; } = "moonglade-images";

    public string SecondaryContainerName { get; set; } = "moonglade-images-origin";
}