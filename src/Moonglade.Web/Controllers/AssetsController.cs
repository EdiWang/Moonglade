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
using Moonglade.Web.SiteIconGenerator;
using NUglify;

namespace Moonglade.Web.Controllers
{
    public class AssetsController : Controller
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogImageStorage _imageStorage;
        private readonly ISiteIconGenerator _siteIconGenerator;
        private readonly IWebHostEnvironment _env;
        private readonly ImageStorageSettings _imageStorageSettings;
        private readonly AppSettings _settings;
        private readonly ILogger<AssetsController> _logger;

        private static string SiteIconDirectory => Path.Join(AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString(), "siteicons");

        public AssetsController(
            ILogger<AssetsController> logger,
            IOptions<AppSettings> settings,
            IOptions<ImageStorageSettings> imageStorageSettings,
            IBlogImageStorage imageStorage,
            IBlogConfig blogConfig,
            ISiteIconGenerator siteIconGenerator,
            IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _blogConfig = blogConfig;
            _siteIconGenerator = siteIconGenerator;
            _env = env;
            _imageStorage = imageStorage;
            _imageStorageSettings = imageStorageSettings.Value;
            _logger = logger;
        }

        #region Blog Post Images

        [Route(@"image/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp))}")]
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

                return Json(new
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

        [Route("captcha-image")]
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

        [Route("avatar")]
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
        [Route(@"/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|ico))}")]
        public async Task<IActionResult> SiteIcon(string filename)
        {
            // BUG:
            // When `RefreshSiteIconCache()` is not finished, not all icons are there
            // And the second request hits this `if` statement
            // It is `false`, so the requested filename will be 404 because it's not there at this time
            if (!Directory.Exists(SiteIconDirectory) || !Directory.GetFiles(SiteIconDirectory).Any())
            {
                await RefreshSiteIconCache();
            }

            var iconPath = Path.Join(SiteIconDirectory, filename.ToLower());
            if (!System.IO.File.Exists(iconPath)) return NotFound();

            var contentType = "image/png";
            var ext = Path.GetExtension(filename);
            contentType = ext switch
            {
                ".png" => "image/png",
                ".ico" => "image/x-icon",
                _ => contentType
            };
            return PhysicalFile(iconPath, contentType);
        }

        [Authorize]
        [Route("siteicon")]
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

        private async Task RefreshSiteIconCache()
        {
            try
            {
                string iconTemplatPath = string.Empty;

                try
                {
                    var data = await _blogConfig.GetAssetDataAsync(AssetId.SiteIconBase64);

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        var siteIconBytes = Convert.FromBase64String(data);

                        await using (var ms = new MemoryStream(siteIconBytes))
                        {
                            var image = System.Drawing.Image.FromStream(ms);
                            if (image.Height != image.Width)
                            {
                                throw new InvalidOperationException("Invalid Site Icon Data");
                            }
                        }

                        var p = Path.Join(SiteIconDirectory, "siteicon.png");
                        if (!Directory.Exists(SiteIconDirectory))
                        {
                            Directory.CreateDirectory(SiteIconDirectory);
                        }

                        await System.IO.File.WriteAllBytesAsync(p, siteIconBytes);
                        iconTemplatPath = p;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error {nameof(RefreshSiteIconCache)}()", e);
                }

                if (string.IsNullOrWhiteSpace(iconTemplatPath))
                {
                    _logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image.");
                    iconTemplatPath = Path.Join($"{_env.WebRootPath}", "images", "siteicon-default.png");
                }

                if (System.IO.File.Exists(iconTemplatPath))
                {
                    _siteIconGenerator.GenerateIcons(iconTemplatPath, SiteIconDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {nameof(RefreshSiteIconCache)}()", ex);
            }
        }

        #endregion

        // Credits: https://github.com/Anduin2017/Blog
        [ResponseCache(Duration = 3600)]
        [Route("/manifest.json")]
        public async Task<IActionResult> Manifest(
            [FromServices] IWebHostEnvironment hostEnvironment,
            [FromServices] IOptions<List<ManifestIcon>> manifestIcons)
        {
            var themeColor = await Helper.GetThemeColorAsync(hostEnvironment.WebRootPath, _blogConfig.GeneralSettings.ThemeFileName);

            var model = new ManifestModel
            {
                ShortName = _blogConfig.GeneralSettings.SiteTitle,
                Name = _blogConfig.GeneralSettings.SiteTitle,
                Description = _blogConfig.GeneralSettings.SiteTitle,
                StartUrl = "/",
                Icons = manifestIcons?.Value,
                BackgroundColor = themeColor,
                ThemeColor = themeColor,
                Display = "standalone",
                Orientation = "portrait"
            };
            return Json(model);
        }

        [FeatureGate(FeatureFlags.Foaf)]
        [ResponseCache(Duration = 3600)]
        [Route("foaf.xml")]
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

        [HttpGet("custom.css")]
        public IActionResult CustomCss()
        {
            if (!_blogConfig.CustomStyleSheetSettings.EnableCustomCss)
            {
                return NotFound();
            }

            var cssCode = _blogConfig.CustomStyleSheetSettings.CssCode;
            if (cssCode.Length > 10240)
            {
                return Conflict("CSS Code length exceeded 10240 characters, refuse to load");
            }

            var uglifiedCss = Uglify.Css(cssCode);
            if (uglifiedCss.HasErrors)
            {
                foreach (var err in uglifiedCss.Errors)
                {
                    ModelState.AddModelError("CSS", err.ToString());
                }

                return Conflict("Invalid CSS Code");
            }

            return Content(uglifiedCss.Code, "text/css");
        }
    }
}