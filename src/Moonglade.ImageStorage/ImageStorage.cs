using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageSettings
{
    public string[] AllowedExtensions { get; set; }

    public string Provider { get; set; }

    public string FileSystemPath { get; set; }

    public string[] WatermarkSkipExtensions { get; set; }

    public AzureStorageSettings AzureStorageSettings { get; set; }

    public MinioStorageSettings MinioStorageSettings { get; set; }

    public QiniuStorageSettings QiniuStorageSettings { get; set; }
}