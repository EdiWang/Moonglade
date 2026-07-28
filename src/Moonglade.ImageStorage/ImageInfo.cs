namespace Moonglade.ImageStorage;

public class ImageInfo
{
    public string ImageExtensionName { get; set; }

    public string ContentType { get; set; }

    public long ContentLength { get; set; }

    public DateTimeOffset? LastModifiedUtc { get; set; }

    public string EntityTag { get; set; }

    public string ImageContentType => string.IsNullOrWhiteSpace(ContentType)
        ? GetContentType(ImageExtensionName)
        : ContentType;

    public static string GetContentType(string extension)
    {
        var normalized = extension?.TrimStart('.').ToLowerInvariant();

        return normalized switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "bmp" => "image/bmp",
            "tiff" or "tif" => "image/tiff",
            "ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }
}
