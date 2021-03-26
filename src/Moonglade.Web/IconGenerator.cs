using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Web
{
    public interface ISiteIconGenerator
    {
        Task GenerateIcons();
        byte[] GetIcon(string fileName);
        void Dirty();
    }

    public class MemoryStreamIconGenerator : ISiteIconGenerator
    {
        public ConcurrentDictionary<string, byte[]> SiteIconDictionary { get; set; }

        private readonly IBlogConfig _blogConfig;
        private readonly ILogger<MemoryStreamIconGenerator> _logger;
        private readonly IWebHostEnvironment _env;

        private bool _hasInitialized;

        public MemoryStreamIconGenerator(
            IBlogConfig blogConfig, ILogger<MemoryStreamIconGenerator> logger, IWebHostEnvironment env)
        {
            _blogConfig = blogConfig;
            _logger = logger;
            _env = env;
            SiteIconDictionary = new();
        }

        public async Task GenerateIcons()
        {
            if (_hasInitialized) return;

            try
            {
                var data = await _blogConfig.GetAssetDataAsync(AssetId.SiteIconBase64);
                byte[] buffer;

                // Fall back to default image
                if (string.IsNullOrWhiteSpace(data))
                {
                    _logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image.");

                    var defaultImagePath = Path.Join($"{_env.WebRootPath}", "images", "siteicon-default.png");
                    if (!File.Exists(defaultImagePath))
                    {
                        throw new FileNotFoundException("Can not find source image for generating favicons.", defaultImagePath);
                    }

                    var ext = Path.GetExtension(defaultImagePath);
                    if (ext is not null && ext.ToLower() is not ".png")
                    {
                        throw new FormatException("Source file is not an PNG image.");
                    }

                    buffer = await File.ReadAllBytesAsync(defaultImagePath);
                }
                else
                {
                    buffer = Convert.FromBase64String(data);
                }

                await using (var ms = new MemoryStream(buffer))
                {
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

                            SiteIconDictionary.TryAdd(fileName, bytes);
                        }
                    }

                    var icon1Bytes = ResizeImage(ms, 192, 192, ImageFormat.Png);
                    SiteIconDictionary.TryAdd("apple-icon.png", icon1Bytes);

                    var icon2Bytes = ResizeImage(ms, 192, 192, ImageFormat.Png);
                    SiteIconDictionary.TryAdd("apple-icon-precomposed.png", icon2Bytes);

                    var icoBytes = GenerateIco(SiteIconDictionary["favicon-16x16.png"]);
                    SiteIconDictionary.TryAdd("favicon.ico", icoBytes);
                }

                _hasInitialized = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public byte[] GetIcon(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return SiteIconDictionary.ContainsKey(fileName) ? SiteIconDictionary[fileName] : null;
        }

        public void Dirty()
        {
            _hasInitialized = false;
        }

        private static byte[] GenerateIco(byte[] fromBytes)
        {
            var stream = new MemoryStream(fromBytes);
            using (stream)
            {
                using var image = new Bitmap(stream);

                // Wrong color and incorrect ico image, but since mordern browsers only use 'favicon-16x16.png', this .ico file is considerable fine.
                var icon = Icon.FromHandle(image.GetHicon());

                using var memoryStream = new MemoryStream();
                icon.Save(memoryStream);
                memoryStream.Flush();

                return memoryStream.ToArray();
            }
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
