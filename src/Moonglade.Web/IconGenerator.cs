using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;

namespace Moonglade.Web;

public static class MemoryStreamIconGenerator
{
    public static ConcurrentDictionary<string, byte[]> SiteIconDictionary { get; set; } = new();

    public static void GenerateIcons(string base64Data, string webRootPath, ILogger logger)
    {
        byte[] buffer;

        // Fall back to default image
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image.");

            // Credit: Vector Market (siteicon-default.png)
            var defaultIconImage = Path.Join($"{webRootPath}", "images", "siteicon-default.png");
            if (!File.Exists(defaultIconImage))
            {
                throw new FileNotFoundException("Can not find source image for generating favicons.", defaultIconImage);
            }

            var ext = Path.GetExtension(defaultIconImage);
            if (ext is not null && ext.ToLower() is not ".png")
            {
                throw new FormatException("Source file is not an PNG image.");
            }

            buffer = File.ReadAllBytes(defaultIconImage);
        }
        else
        {
            buffer = Convert.FromBase64String(base64Data);
        }

        using var ms = new MemoryStream(buffer);
        using var image = Image.Load(ms);
        if (image.Height != image.Width)
        {
            throw new InvalidOperationException("Invalid Site Icon Data");
        }

        var dic = new Dictionary<string, int[]>
        {
            { "android-icon-", new[] { 144, 192 } },
            { "favicon-", new[] { 16, 32, 96 } },
            { "apple-icon-", new[] { 180 } }
        };

        foreach (var (key, value) in dic)
        {
            foreach (var size in value)
            {
                var fileName = $"{key}{size}x{size}.png";
                var bytes = ResizeImage(image, size, size);

                SiteIconDictionary.TryAdd(fileName, bytes);
            }
        }

        var icon1Bytes = ResizeImage(image, 180, 180);
        SiteIconDictionary.TryAdd("apple-icon.png", icon1Bytes);
    }

    public static byte[] GetIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        return SiteIconDictionary.GetValueOrDefault(fileName);
    }

    private static byte[] ResizeImage(Image image, int toWidth, int toHeight)
    {
        image.Mutate(x => x.Resize(toWidth, toHeight));
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}