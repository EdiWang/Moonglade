using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageSettings
{
    public string[] AllowedExtensions { get; set; }

    public string Provider { get; set; }

    public WatermarkSettings Watermark { get; set; }

    public AzureStorageSettings AzureStorageSettings { get; set; }

    public FileSystemSettings FileSystemSettings { get; set; }

    public MinioStorageSettings MinioStorageSettings { get; set; }

    public QiniuStorageSettings QiniuStorageSettings { get; set; }
}

public class WatermarkSettings
{
    public string[] NoWatermarkExtensions { get; set; }
    public int[] WatermarkARGB { get; set; }
    public int WatermarkSkipPixel { get; set; }
}