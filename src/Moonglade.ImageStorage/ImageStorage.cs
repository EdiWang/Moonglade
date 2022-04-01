using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageSettings
{
    public string[] AllowedExtensions { get; set; }

    public string Provider { get; set; }

    public string FileSystemPath { get; set; }

    public WatermarkSettings Watermark { get; set; }

    public AzureStorageSettings AzureStorageSettings { get; set; }

    public MinioStorageSettings MinioStorageSettings { get; set; }

    public QiniuStorageSettings QiniuStorageSettings { get; set; }
}

public class WatermarkSettings
{
    public string[] SkipExtensions { get; set; }
    public int WatermarkSkipPixel { get; set; }
}