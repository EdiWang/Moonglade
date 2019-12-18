using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Edi.Captcha;
using Edi.ImageWatermark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;
using Moonglade.ImageStorage;
using Moonglade.Model.Settings;
using Moonglade.Core;

namespace Moonglade.Web.Controllers
{
    public class ImageController : MoongladeController
    {
        private readonly IAsyncImageStorageProvider _imageStorageProvider;

        private readonly IBlogConfig _blogConfig;

        private readonly CDNSettings _cdnSettings;

        public ImageController(
            ILogger<ImageController> logger,
            IOptions<AppSettings> settings,
            IOptions<ImageStorageSettings> imageStorageSettings,
            IAsyncImageStorageProvider imageStorageProvider,
            IBlogConfig blogConfig)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _imageStorageProvider = imageStorageProvider;
            _cdnSettings = imageStorageSettings.Value?.CDNSettings;
        }

        [ResponseCache(Duration = 3600)]
        [Route(@"/{filename:regex((?!-)([[a-z0-9-]]+)\.(png|ico))}")]
        public IActionResult Favicon(string filename)
        {
            var faviconDirectory = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\favicons";
            var iconPath = Path.Combine(faviconDirectory, filename.ToLower());
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
        [HttpPost("image/upload"), IgnoreAntiforgeryToken]
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
                    return Json(new { location = $"/uploads/{response.Item}" });
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

        [Route("get-captcha-image")]
        public IActionResult GetCaptchaImage([FromServices] ISessionBasedCaptcha captcha)
        {
            var s = captcha.GenerateCaptchaImageFileStream(HttpContext.Session,
                AppSettings.CaptchaSettings.ImageWidth,
                AppSettings.CaptchaSettings.ImageHeight);
            return s;
        }

        [Route("avatar")]
        public IActionResult Avatar([FromServices] IMemoryCache cache)
        {
            var fallbackImageFile =
                $@"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}\wwwroot\images\avatar-placeholder.png";

            if (string.IsNullOrWhiteSpace(_blogConfig.BlogOwnerSettings.AvatarBase64))
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            try
            {
                return cache.GetOrCreate(StaticCacheKeys.Avatar, entry =>
                {
                    Logger.LogTrace("Avatar not on cache, getting new avatar image...");
                    var avatarBytes = Convert.FromBase64String(_blogConfig.BlogOwnerSettings.AvatarBase64);
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
    }
}