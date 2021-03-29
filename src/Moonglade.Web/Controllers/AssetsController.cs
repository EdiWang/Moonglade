using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Edi.Captcha;
using Edi.ImageWatermark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.FriendLink;
using Moonglade.ImageStorage;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
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

                var imageEntry = await cache.GetOrCreateAsync(filename, async entry =>
                {
                    _logger.LogTrace($"Image file {filename} not on cache, fetching image...");

                    entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Image"]);
                    var imgBytesResponse = await _imageStorage.GetAsync(filename);
                    return imgBytesResponse;
                });

                if (null == imageEntry) return NotFound();

                return File(imageEntry.ImageBytes, imageEntry.ImageContentType);
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
                var allowedImageFormats = _imageStorageSettings.AllowedExtensions;

                if (null == allowedImageFormats || !allowedImageFormats.Any())
                {
                    throw new InvalidDataException($"{nameof(ImageStorageSettings.AllowedExtensions)} is empty.");
                }

                if (!allowedImageFormats.Contains(ext))
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
                    if (null == _imageStorageSettings.NoWatermarkExtensions
                        || _imageStorageSettings.NoWatermarkExtensions.All(
                            p => string.Compare(p, ext, StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        using var watermarker = new ImageWatermarker(stream, ext);
                        watermarker.SkipImageSize(_settings.WatermarkSkipPixel);

                        // Get ARGB values
                        var colorArray = _settings.WatermarkARGB;
                        if (colorArray.Length != 4)
                        {
                            throw new InvalidDataException($"'{nameof(_settings.WatermarkARGB)}' must be an integer array with 4 items.");
                        }

                        if (colorArray.Any(c => !IsValidColorValue(c)))
                        {
                            throw new InvalidDataException($"'{nameof(_settings.WatermarkARGB)}' values must all fall in range 0-255.");
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

        [HttpGet("captcha-image")]
        public IActionResult CaptchaImage([FromServices] ISessionBasedCaptcha captcha)
        {
            var w = _settings.CaptchaSettings.ImageWidth;
            var h = _settings.CaptchaSettings.ImageHeight;

            // prevent crazy size
            if (w > 640) w = 640;
            if (h > 480) h = 480;

            var s = captcha.GenerateCaptchaImageFileStream(HttpContext.Session, w, h);
            return s;
        }

        [HttpGet("avatar")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> Avatar([FromServices] IBlogCache cache)
        {
            var data = await _blogConfig.GetAssetDataAsync(AssetId.AvatarBase64);
            var fallbackImageFile = Path.Join($"{_env.WebRootPath}", "images", "default-avatar.png");
            if (string.IsNullOrWhiteSpace(data))
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            try
            {
                return cache.GetOrCreate(CacheDivision.General, "avatar", _ =>
                {
                    _logger.LogTrace("Avatar not on cache, getting new avatar image...");
                    var avatarBytes = Convert.FromBase64String(data);
                    return File(avatarBytes, "image/png");
                });
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

        [FeatureGate(FeatureFlags.Foaf)]
        [ResponseCache(Duration = 3600)]
        [HttpGet("foaf.xml")]
        public async Task<IActionResult> Foaf(
            [FromServices] IFoafWriter foafWriter,
            [FromServices] IFriendLinkService friendLinkService,
            [FromServices] LinkGenerator linkGenerator)
        {
            static Uri GetUri(HttpRequest request)
            {
                return new(string.Concat(
                    request.Scheme,
                    "://",
                    request.Host.HasValue
                        ? (request.Host.Value.IndexOf(",", StringComparison.Ordinal) > 0
                            ? "MULTIPLE-HOST"
                            : request.Host.Value)
                        : "UNKNOWN-HOST",
                    request.Path.HasValue ? request.Path.Value : string.Empty,
                    request.QueryString.HasValue ? request.QueryString.Value : string.Empty));
            }

            var friends = await friendLinkService.GetAllAsync();
            var foafDoc = new FoafDoc
            {
                Name = _blogConfig.GeneralSettings.OwnerName,
                BlogUrl = Helper.ResolveRootUrl(HttpContext, _blogConfig.GeneralSettings.CanonicalPrefix, true),
                Email = _blogConfig.GeneralSettings.OwnerEmail,
                PhotoUrl = linkGenerator.GetUriByAction(HttpContext, "Avatar", "Assets")
            };
            var requestUrl = GetUri(Request).ToString();
            var xml = await foafWriter.GetFoafData(foafDoc, requestUrl, friends);

            return Content(xml, FoafWriter.ContentType);
        }
    }
}