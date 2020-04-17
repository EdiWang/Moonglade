using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edi.Captcha;
using Edi.ImageWatermark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using Moonglade.Web.SiteIconGenerator;

namespace Moonglade.Web.Controllers
{
    public class AssetsController : MoongladeController
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IAsyncImageStorageProvider _imageStorageProvider;
        private readonly ISiteIconGenerator _siteIconGenerator;
        private readonly CDNSettings _cdnSettings;

        public AssetsController(
            ILogger<AssetsController> logger,
            IOptions<AppSettings> settings,
            IOptions<ImageStorageSettings> imageStorageSettings,
            IAsyncImageStorageProvider imageStorageProvider,
            IBlogConfig blogConfig,
            ISiteIconGenerator siteIconGenerator) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _siteIconGenerator = siteIconGenerator;
            _imageStorageProvider = imageStorageProvider;
            _cdnSettings = imageStorageSettings.Value?.CDNSettings;
        }

        #region Blog Post Images

        [Route(@"uploads/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp))}")]
        public async Task<IActionResult> GetImageAsync(string filename, [FromServices] IMemoryCache cache)
        {
            try
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (filename.IndexOfAny(invalidChars) >= 0)
                {
                    Logger.LogWarning($"Invalid filename attempt '{filename}'.");
                    return BadRequest("invalid filename");
                }

                Logger.LogTrace($"Requesting image file {filename}");

                if (_cdnSettings.GetImageByCDNRedirect)
                {
                    var imageUrl = Utils.CombineUrl(_cdnSettings.CDNEndpoint, filename);
                    return Redirect(imageUrl);
                }

                var imageEntry = await cache.GetOrCreateAsync(filename, async entry =>
                {
                    Logger.LogTrace($"Image file {filename} not on cache, fetching image...");

                    entry.SlidingExpiration = TimeSpan.FromMinutes(AppSettings.ImageCacheSlidingExpirationMinutes);
                    var imgBytesResponse = await _imageStorageProvider.GetAsync(filename);
                    return imgBytesResponse;
                });

                if (imageEntry.IsSuccess)
                {
                    return File(imageEntry.Item.ImageBytes, imageEntry.Item.ImageContentType);
                }

                Logger.LogError($"Error getting image, filename: {filename}, {imageEntry.Message}");

                return _blogConfig.ContentSettings.UseFriendlyNotFoundImage
                    ? (IActionResult)File("~/images/image-not-found.png", "image/png")
                    : NotFound();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error requesting image {filename}");
                return ServerError();
            }
        }

        [Authorize]
        [HttpPost("upload-image"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadImageAsync(IFormFile file, [FromServices] IFileNameGenerator fileNameGenerator)
        {
            try
            {
                if (null == file)
                {
                    Logger.LogError("file is null.");
                    return BadRequest();
                }

                if (file.Length <= 0) return BadRequest();

                var name = Path.GetFileName(file.FileName);
                if (name == null) return BadRequest();

                var ext = Path.GetExtension(name).ToLower();
                var allowedImageFormats = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                if (!allowedImageFormats.Contains(ext))
                {
                    Logger.LogError($"Invalid file extension: {ext}");
                    return BadRequest();
                }

                var primaryFileName = fileNameGenerator.GetFileName(name);
                var secondaryFieName = fileNameGenerator.GetFileName(name, "origin");

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                // Add watermark
                MemoryStream watermarkedStream = null;
                if (_blogConfig.WatermarkSettings.IsEnabled && ext != ".gif")
                {
                    using var watermarker = new ImageWatermarker(stream, ext)
                    {
                        SkipWatermarkForSmallImages = true,
                        SmallImagePixelsThreshold = Constants.SmallImagePixelsThreshold
                    };
                    Logger.LogInformation($"Adding watermark onto image '{primaryFileName}'");

                    watermarkedStream = watermarker.AddWatermark(
                        _blogConfig.WatermarkSettings.WatermarkText,
                        Color.FromArgb(128, 128, 128, 128),
                        WatermarkPosition.BottomRight,
                        15,
                        _blogConfig.WatermarkSettings.FontSize);
                }

                var response = await _imageStorageProvider.InsertAsync(primaryFileName,
                    watermarkedStream != null ?
                        watermarkedStream.ToArray() :
                        stream.ToArray());

                if (_blogConfig.WatermarkSettings.KeepOriginImage)
                {
                    var arr = stream.ToArray();
                    _ = Task.Run(async () => await _imageStorageProvider.InsertAsync(secondaryFieName, arr));
                }

                Logger.LogInformation($"Image '{primaryFileName}' uloaded.");

                if (response.IsSuccess)
                {
                    return Json(new
                    {
                        location = $"/uploads/{response.Item}",
                        filename = response.Item
                    });
                }
                Logger.LogError(response.Message);
                return ServerError();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading image.");
                return ServerError();
            }
        }

        #endregion

        [Route("get-captcha-image")]
        public IActionResult GetCaptchaImage([FromServices] ISessionBasedCaptcha captcha)
        {
            var s = captcha.GenerateCaptchaImageFileStream(HttpContext.Session,
                AppSettings.CaptchaSettings.ImageWidth,
                AppSettings.CaptchaSettings.ImageHeight);
            return s;
        }

        [Route("avatar")]
        [ResponseCache(Duration = 300)]
        public IActionResult Avatar([FromServices] IMemoryCache cache)
        {
            var fallbackImageFile = Path.Join($"{SiteRootDirectory}", "wwwroot", "images", "default-avatar.png");
            if (string.IsNullOrWhiteSpace(_blogConfig.GeneralSettings.AvatarBase64))
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            try
            {
                return cache.GetOrCreate(StaticCacheKeys.Avatar, entry =>
                {
                    Logger.LogTrace("Avatar not on cache, getting new avatar image...");
                    var avatarBytes = Convert.FromBase64String(_blogConfig.GeneralSettings.AvatarBase64);
                    return File(avatarBytes, "image/png");
                });
            }
            catch (FormatException e)
            {
                Logger.LogError($"Error {nameof(Avatar)}(), Invalid Base64 string", e);
                return PhysicalFile(fallbackImageFile, "image/png");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error {nameof(Avatar)}()", ex);
                return new EmptyResult();
            }
        }

        #region Site Icon

        [ResponseCache(Duration = 3600)]
        [Route(@"/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|ico))}")]
        public IActionResult SiteIcon(string filename)
        {
            if (!Directory.Exists(SiteIconDirectory) || !Directory.GetFiles(SiteIconDirectory).Any())
            {
                RefreshSiteIconCache();
            }

            var iconPath = Path.Join(SiteIconDirectory, filename.ToLower());
            if (System.IO.File.Exists(iconPath))
            {
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

            return NotFound();
        }

        [Authorize]
        [Route("siteicon")]
        public IActionResult SiteIconOrigin()
        {
            var fallbackImageFile = Path.Join($"{SiteRootDirectory}", "wwwroot", "siteicon-default.png");
            if (string.IsNullOrWhiteSpace(_blogConfig.GeneralSettings.SiteIconBase64))
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            try
            {
                var siteIconBytes = Convert.FromBase64String(_blogConfig.GeneralSettings.SiteIconBase64);
                return File(siteIconBytes, "image/png");
            }
            catch (FormatException e)
            {
                Logger.LogError($"Error {nameof(SiteIconOrigin)}(), Invalid Base64 string", e);
                return PhysicalFile(fallbackImageFile, "image/png");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error {nameof(SiteIconOrigin)}()", ex);
                return new EmptyResult();
            }
        }

        private void RefreshSiteIconCache()
        {
            try
            {
                string iconTemplatPath = string.Empty;

                try
                {
                    if (!string.IsNullOrWhiteSpace(_blogConfig.GeneralSettings.SiteIconBase64))
                    {
                        var siteIconBytes = Convert.FromBase64String(_blogConfig.GeneralSettings.SiteIconBase64);

                        using (var ms = new MemoryStream(siteIconBytes))
                        {
                            var image = Image.FromStream(ms);
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

                        System.IO.File.WriteAllBytes(p, siteIconBytes);
                        iconTemplatPath = p;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error {nameof(RefreshSiteIconCache)}()", e);
                }

                if (string.IsNullOrWhiteSpace(iconTemplatPath))
                {
                    Logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image.");
                    iconTemplatPath = Path.Join($"{SiteRootDirectory}", "wwwroot", "siteicon-default.png");
                }

                if (System.IO.File.Exists(iconTemplatPath))
                {
                    _siteIconGenerator.GenerateIcons(iconTemplatPath, SiteIconDirectory);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error {nameof(RefreshSiteIconCache)}()", ex);
            }
        }

        #endregion

        [ResponseCache(Duration = 3600)]
        [Route("/robots.txt")]
        public IActionResult RobotsTxt()
        {
            var robotsTxtContent = _blogConfig.AdvancedSettings.RobotsTxtContent;
            if (string.IsNullOrWhiteSpace(robotsTxtContent))
            {
                Logger.LogWarning("No content in robots.txt configuration.");
                return NotFound();
            }

            return Content(_blogConfig.AdvancedSettings.RobotsTxtContent, "text/plain", Encoding.UTF8);
        }

        // Credits: https://github.com/Anduin2017/Blog
        [ResponseCache(Duration = 3600)]
        [Route("/manifest.json")]
        public async Task<IActionResult> Manifest([FromServices]IWebHostEnvironment hostEnvironment)
        {
            var themeColor = await Utils.GetThemeColorAsync(hostEnvironment.WebRootPath, _blogConfig.GeneralSettings.ThemeFileName);

            var model = new ManifestModel
            {
                ShortName = _blogConfig.GeneralSettings.SiteTitle,
                Name = _blogConfig.GeneralSettings.SiteTitle,
                Description = _blogConfig.GeneralSettings.SiteTitle,
                StartUrl = "/",
                Icons = new List<ManifestIcon>
                {
                    new ManifestIcon("/android-icon-{0}.png",36,"0.75"),
                    new ManifestIcon("/android-icon-{0}.png",48,"1.0"),
                    new ManifestIcon("/android-icon-{0}.png",72,"1.5"),
                    new ManifestIcon("/android-icon-{0}.png",96,"2.0"),
                    new ManifestIcon("/android-icon-{0}.png",144,"3.0"),
                    new ManifestIcon("/android-icon-{0}.png",192,"4.0")
                },
                BackgroundColor = themeColor,
                ThemeColor = themeColor,
                Display = "standalone",
                Orientation = "portrait"
            };
            return Json(model);
        }
    }
}