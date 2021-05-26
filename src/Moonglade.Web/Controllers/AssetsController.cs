using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Edi.ImageWatermark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.ImageStorage;
using Moonglade.Utils;

namespace Moonglade.Web.Controllers
{
    public class AssetsController : ControllerBase
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogImageStorage _imageStorage;
        private readonly IWebHostEnvironment _env;
        private readonly ImageStorageSettings _imageStorageSettings;
        private readonly AppSettings _settings;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(
            ILogger<AssetsController> logger,
            IOptions<AppSettings> settings,
            IOptions<ImageStorageSettings> imageStorageSettings,
            IBlogImageStorage imageStorage,
            IBlogConfig blogConfig,
            IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _blogConfig = blogConfig;
            _env = env;
            _imageStorage = imageStorage;
            _imageStorageSettings = imageStorageSettings.Value;
            _logger = logger;
        }

        #region Blog Post Images

        [HttpGet(@"image/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp))}")]
        public async Task<IActionResult> Image(string filename, [FromServices] IMemoryCache cache)
        {
            try
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (filename.IndexOfAny(invalidChars) >= 0)
                {
                    return BadRequest("invalid filename");
                }

                _logger.LogTrace($"Requesting image file {filename}");

                if (_blogConfig.AdvancedSettings.EnableCDNRedirect)
                {
                    var imageUrl = _blogConfig.AdvancedSettings.CDNEndpoint.CombineUrl(filename);
                    return Redirect(imageUrl);
                }

                var image = await cache.GetOrCreateAsync(filename, async entry =>
                {
                    _logger.LogTrace($"Image file {filename} not on cache, fetching image...");

                    entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Image"]);
                    var imageInfo = await _imageStorage.GetAsync(filename);
                    return imageInfo;
                });

                if (null == image) return NotFound();

                return File(image.ImageBytes, image.ImageContentType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error requesting image {filename}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize]
        [HttpPost("image"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Image(IFormFile file, [FromServices] IFileNameGenerator fileNameGenerator)
        {
            static bool IsValidColorValue(int colorValue)
            {
                return colorValue is >= 0 and <= 255;
            }

            try
            {
                if (file is null or { Length: <= 0 })
                {
                    _logger.LogError("file is null.");
                    return BadRequest();
                }

                var name = Path.GetFileName(file.FileName);

                var ext = Path.GetExtension(name).ToLower();
                var allowedExtensions = _imageStorageSettings.AllowedExtensions;

                if (null == allowedExtensions || !allowedExtensions.Any())
                {
                    throw new InvalidDataException($"{nameof(ImageStorageSettings.AllowedExtensions)} is empty.");
                }

                if (!allowedExtensions.Contains(ext))
                {
                    _logger.LogError($"Invalid file extension: {ext}");
                    return BadRequest();
                }

                var primaryFileName = fileNameGenerator.GetFileName(name);
                var secondaryFieName = fileNameGenerator.GetFileName(name, "origin");

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                // Add watermark
                MemoryStream watermarkedStream = null;
                if (_blogConfig.WatermarkSettings.IsEnabled)
                {
                    if (null == _imageStorageSettings.Watermark.NoWatermarkExtensions
                        || _imageStorageSettings.Watermark.NoWatermarkExtensions.All(
                            p => string.Compare(p, ext, StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        using var watermarker = new ImageWatermarker(stream, ext);
                        watermarker.SkipImageSize(_imageStorageSettings.Watermark.WatermarkSkipPixel);

                        // Get ARGB values
                        var colorArray = _imageStorageSettings.Watermark.WatermarkARGB;
                        if (colorArray.Length != 4)
                        {
                            throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' must be an integer array with 4 items.");
                        }

                        if (colorArray.Any(c => !IsValidColorValue(c)))
                        {
                            throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' values must all fall in range 0-255.");
                        }

                        watermarkedStream = watermarker.AddWatermark(
                            _blogConfig.WatermarkSettings.WatermarkText,
                            Color.FromArgb(colorArray[0], colorArray[1], colorArray[2], colorArray[3]),
                            WatermarkPosition.BottomRight,
                            15,
                            _blogConfig.WatermarkSettings.FontSize);
                    }
                    else
                    {
                        _logger.LogInformation($"Skipped watermark for extension name: {ext}");
                    }
                }

                var finalFileName = await _imageStorage.InsertAsync(primaryFileName,
                    watermarkedStream is not null ?
                        watermarkedStream.ToArray() :
                        stream.ToArray());

                if (_blogConfig.WatermarkSettings.KeepOriginImage)
                {
                    var arr = stream.ToArray();
                    _ = Task.Run(async () => await _imageStorage.InsertAsync(secondaryFieName, arr));
                }

                _logger.LogInformation($"Image '{primaryFileName}' uloaded.");

                return Ok(new
                {
                    location = $"/image/{finalFileName}",
                    filename = finalFileName
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading image.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        [HttpGet("avatar")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> Avatar([FromServices] IBlogCache cache)
        {
            var fallbackImageFile = Path.Join($"{_env.WebRootPath}", "images", "default-avatar.png");

            try
            {
                var bytes = await cache.GetOrCreateAsync(CacheDivision.General, "avatar", async _ =>
                {
                    _logger.LogTrace("Avatar not on cache, getting new avatar image...");

                    var data = await _blogConfig.GetAssetDataAsync(AssetId.AvatarBase64);
                    if (string.IsNullOrWhiteSpace(data)) return null;

                    var avatarBytes = Convert.FromBase64String(data);
                    return avatarBytes;
                });

                if (null == bytes)
                {
                    return PhysicalFile(fallbackImageFile, "image/png");
                }

                return File(bytes, "image/png");
            }
            catch (FormatException e)
            {
                _logger.LogError($"Error {nameof(Avatar)}(), Invalid Base64 string", e);
                return PhysicalFile(fallbackImageFile, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {nameof(Avatar)}()", ex);
                return new EmptyResult();
            }
        }

        #region Site Icon

        [ResponseCache(Duration = 3600)]
        [HttpHead(@"/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
        [HttpGet(@"/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
        public IActionResult SiteIcon(string filename)
        {
            var iconBytes = MemoryStreamIconGenerator.GetIcon(filename);
            if (iconBytes is null) return NotFound();

            var contentType = "image/png";
            var ext = Path.GetExtension(filename);
            contentType = ext switch
            {
                ".png" => "image/png",
                ".ico" => "image/x-icon",
                _ => contentType
            };
            return File(iconBytes, contentType);
        }

        [Authorize]
        [HttpGet("siteicon")]
        public async Task<IActionResult> SiteIconOrigin()
        {
            var data = await _blogConfig.GetAssetDataAsync(AssetId.SiteIconBase64);
            var fallbackImageFile = Path.Join($"{_env.WebRootPath}", "images", "siteicon-default.png");
            if (string.IsNullOrWhiteSpace(data))
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            try
            {
                var siteIconBytes = Convert.FromBase64String(data);
                return File(siteIconBytes, "image/png");
            }
            catch (FormatException e)
            {
                _logger.LogError($"Error {nameof(SiteIconOrigin)}(), Invalid Base64 string", e);
                return PhysicalFile(fallbackImageFile, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {nameof(SiteIconOrigin)}()", ex);
                return new EmptyResult();
            }
        }

        #endregion
    }
}