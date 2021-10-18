using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Moonglade.Web
{
    public static class MemoryStreamIconGenerator
    {
        public static void GenerateIcons(string base64Data, string webRootPath, ILogger logger)
        {
            byte[] buffer;

            // Fall back to default image
            if (string.IsNullOrWhiteSpace(base64Data))
            {
                logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image.");

                var defaultImagePath = Path.Join($"{webRootPath}", "images", "siteicon-default.png");
                if (!File.Exists(defaultImagePath))
                {
                    throw new FileNotFoundException("Can not find source image for generating favicons.", defaultImagePath);
                }

                var ext = Path.GetExtension(defaultImagePath);
                if (ext is not null && ext.ToLower() is not ".png")
                {
                    throw new FormatException("Source file is not an PNG image.");
                }

                buffer = File.ReadAllBytes(defaultImagePath);
            }
            else
            {
                buffer = Convert.FromBase64String(base64Data);
            }

            using var ms = new MemoryStream(buffer);
            var image = Image.FromStream(ms);
            if (image.Height != image.Width)
            {
                throw new InvalidOperationException("Invalid Site Icon Data");
            }

            var dic = new Dictionary<string, int[]>
            {
                { "android-icon-", new[] { 36, 48, 72, 96, 144, 192 } },
                { "favicon-", new[] { 16, 32, 96 } },
                { "apple-icon-", new[] { 57, 60, 72, 76, 114, 120, 144, 152, 180 } }
            };

            foreach (var (key, value) in dic)
            {
                foreach (var size in value)
                {
                    var fileName = $"{key}{size}x{size}.png";
                    var bytes = ResizeImage(ms, size, size, ImageFormat.Png);

                    Program.SiteIconDictionary.TryAdd(fileName, bytes);
                }
            }

            var icon1Bytes = ResizeImage(ms, 192, 192, ImageFormat.Png);
            Program.SiteIconDictionary.TryAdd("apple-icon.png", icon1Bytes);

            var icon2Bytes = ResizeImage(ms, 192, 192, ImageFormat.Png);
            Program.SiteIconDictionary.TryAdd("apple-icon-precomposed.png", icon2Bytes);
        }

        public static byte[] GetIcon(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return Program.SiteIconDictionary.ContainsKey(fileName) ? Program.SiteIconDictionary[fileName] : null;
        }

        private static byte[] ResizeImage(Stream fromStream, int toWidth, int toHeight, ImageFormat format)
        {
            using var image = new Bitmap(fromStream);
            var resized = new Bitmap(toWidth, toHeight);

            using var graphics = Graphics.FromImage(resized);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(image, 0, 0, toWidth, toHeight);

            using var ms = new MemoryStream();
            resized.Save(ms, format);
            ms.Flush();

            return ms.ToArray();
        }
    }
}
