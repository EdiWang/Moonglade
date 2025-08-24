using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;

namespace Moonglade.Setup;

public static class InMemoryIconGenerator
{
    private static readonly ConcurrentDictionary<string, byte[]> _siteIconDictionary = new();

    // Expose as read-only
    public static IReadOnlyDictionary<string, byte[]> SiteIconDictionary => _siteIconDictionary;

    private const string DefaultIconFileName = "siteicon-default.png";
    private const string ImagesFolder = "images";
    private const string PngExtension = ".png";

    public static void GenerateIcons(string base64Data, string webRootPath, ILogger logger)
    {
        byte[] buffer;

        // Fallback to default image if necessary
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            logger.LogWarning("SiteIconBase64 is empty or not valid, falling back to default image.");

            var defaultIconPath = Path.Combine(webRootPath, ImagesFolder, DefaultIconFileName);
            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("Cannot find source image for generating favicons.", defaultIconPath);
            }

            if (!string.Equals(Path.GetExtension(defaultIconPath), PngExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Source file is not a PNG image.");
            }

            buffer = File.ReadAllBytes(defaultIconPath);
        }
        else
        {
            try
            {
                buffer = Convert.FromBase64String(base64Data);
            }
            catch (FormatException ex)
            {
                logger.LogError(ex, "Base64 string for site icon is invalid. Falling back to default image.");
                var defaultIconPath = Path.Combine(webRootPath, ImagesFolder, DefaultIconFileName);
                buffer = File.ReadAllBytes(defaultIconPath);
            }
        }

        using var ms = new MemoryStream(buffer);
        using var image = Image.Load(ms);

        if (image.Height != image.Width)
        {
            throw new InvalidOperationException("Site icon must be a square image.");
        }

        // Define desired icon sizes
        var iconSizes = new Dictionary<string, int[]>
        {
            { "android-icon-", [144, 192] },
            { "favicon-", [16, 32, 96] },
            { "apple-icon-", [180] }
        };

        // Clear previous icons before generating new ones
        _siteIconDictionary.Clear();

        foreach (var (prefix, sizes) in iconSizes)
        {
            foreach (var size in sizes)
            {
                var fileName = $"{prefix}{size}x{size}.png";
                var resizedBytes = ResizeImage(image, size, size);
                _siteIconDictionary[fileName] = resizedBytes;
            }
        }

        // Add apple-icon.png (180x180)
        var appleIconBytes = ResizeImage(image, 180, 180);
        _siteIconDictionary["apple-icon.png"] = appleIconBytes;
    }

    public static byte[] GetIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        return _siteIconDictionary.TryGetValue(fileName, out var bytes) ? bytes : null;
    }

    private static byte[] ResizeImage(Image image, int width, int height)
    {
        // Clone to avoid mutating the original image
        using var clone = image.Clone(ctx => ctx.Resize(width, height));
        using var ms = new MemoryStream();
        clone.SaveAsPng(ms);
        return ms.ToArray();
    }
}
