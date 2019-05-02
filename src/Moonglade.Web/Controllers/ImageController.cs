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
using Moonglade.Configuration;
using Moonglade.Model;
using Moonglade.ImageStorage;
using Moonglade.Model.Settings;
using Newtonsoft.Json;

namespace Moonglade.Web.Controllers
{
    public class ImageController : MoongladeController
    {
        private readonly ISessionBasedCaptcha _captcha;

        private readonly IAsyncImageStorageProvider _imageStorageProvider;

        private readonly IBlogConfig _blogConfig;

        public ImageController(
            ILogger<ImageController> logger,
            IOptions<AppSettings> settings,
            IMemoryCache memoryCache,
            IAsyncImageStorageProvider imageStorageProvider,
            ISessionBasedCaptcha captcha,
            IBlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService)
            : base(logger, settings, memoryCache: memoryCache)
        {
            _blogConfig = blogConfig;
            _blogConfig.Initialize(blogConfigurationService);

            _imageStorageProvider = imageStorageProvider;
            _captcha = captcha;
        }

        [Route("uploads/{filename}")]
        public async Task<IActionResult> GetImageAsync(string filename)
        {
            try
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (filename.IndexOfAny(invalidChars) > 0)
                {
                    Logger.LogWarning($"Invalid filename attempt '{filename}'.");
                    return BadRequest("invalid filename");
                }

                Logger.LogTrace($"Requesting image file {filename}");

                var imageEntry = await Cache.GetOrCreateAsync(filename, async entry =>
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

                return AppSettings.UsePictureInsteadOfNotFoundResult
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
        [HttpPost]
        [Route("image/upload")]
        public async Task<IActionResult> UploadImageAsync(IFormFile file)
        {
            try
            {
                if (null == file)
                {
                    Logger.LogError("file is null.");
                    return BadRequest();
                }

                if (file.Length > 0)
                {
                    var name = Path.GetFileName(file.FileName);
                    if (name == null) return BadRequest();

                    var ext = Path.GetExtension(name).ToLower();
                    var allowedImageFormats = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                    if (!allowedImageFormats.Contains(ext))
                    {
                        Logger.LogError($"Invalid file extension: {ext}");
                        return BadRequest();
                    }

                    var uid = Guid.NewGuid();
                    IFileNameGenerator gen = new GuidFileNameGenerator(uid);
                    var primaryFileName = gen.GetFileName(name);
                    var secondaryFieName = gen.GetFileName(name, "origin");

                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);

                        // Add watermark
                        MemoryStream watermarkedStream = null;
                        if (_blogConfig.WatermarkSettings.IsEnabled && ext != ".gif")
                        {
                            var watermarker = new ImageWatermarker(stream, ext)
                            {
                                SkipWatermarkForSmallImages = true,
                                SmallImagePixelsThreshold = Constants.SmallImagePixelsThreshold
                            };

                            Logger.LogInformation($"Adding watermark onto image {primaryFileName}");

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

                        Logger.LogInformation("Image Uploaded: " + JsonConvert.SerializeObject(response));

                        if (response.IsSuccess)
                        {
                            return Json(new { location = $"/uploads/{response.Item}" });
                        }
                        Logger.LogError(response.Message);
                        return ServerError();
                    }
                }
                return BadRequest();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading image.");
                return ServerError();
            }
        }

        [Route("get-captcha-image")]
        public IActionResult GetCaptchaImage()
        {
            var s = _captcha.GenerateCaptchaImageFileStream(HttpContext.Session,
                AppSettings.CaptchaSettings.ImageWidth,
                AppSettings.CaptchaSettings.ImageHeight);
            return s;
        }

        [Route("get-avatar")]
        public IActionResult GetBloggerAvatar()
        {
            var fallbackImageFile =
                $@"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}\wwwroot\images\avatar-placeholder.png";

            if (!string.IsNullOrWhiteSpace(_blogConfig.BlogOwnerSettings.AvatarBase64))
            {
                try
                {
                    var avatarEntry = Cache.GetOrCreate(StaticCacheKeys.Avatar, entry =>
                    {
                        Logger.LogTrace("Avatar not on cache, getting new avatar image...");
                        var avatarBytes = Convert.FromBase64String(_blogConfig.BlogOwnerSettings.AvatarBase64);
                        return avatarBytes;
                    });
                    return File(avatarEntry, "image/png");
                }
                catch (FormatException e)
                {
                    Logger.LogError($"Error {nameof(GetBloggerAvatar)}(), Invalid Base64 string", e);
                    return PhysicalFile(fallbackImageFile, "image/png");
                }
            }

            return PhysicalFile(fallbackImageFile, "image/png");
        }
    }
}