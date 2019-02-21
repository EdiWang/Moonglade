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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.ImageStorage;
using Moonglade.Model.Settings;
using Newtonsoft.Json;

namespace Moonglade.Web.Controllers
{
    public class ImageController : MoongladeController
    {
        private readonly ISessionBasedCaptcha _captcha;

        private readonly IAsyncImageStorageProvider _imageStorageProvider;

        private readonly BlogConfig _blogConfig;

        public ImageController(MoongladeDbContext context,
            ILogger<ImageController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache,
            IAsyncImageStorageProvider imageStorageProvider,
            ISessionBasedCaptcha captcha,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _blogConfig = blogConfig;
            _blogConfig.GetConfiguration(blogConfigurationService);

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
                    Logger.LogWarning($"Invalid filename attempt '{filename}' is blocked.");
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
                        return BadRequest();
                    }

                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);

                        // Add watermark
                        MemoryStream watermarkedStream = null;
                        if (AppSettings.WatermarkSettings.IsEnabled && ext != ".gif")
                        {
                            var watermarker = new ImageWatermarker(stream, ext)
                            {
                                SkipWatermarkForSmallImages = true,
                                SmallImagePixelsThreshold = 200 * 200
                            };

                            Logger.LogInformation($"Adding watermark onto image {name}");

                            watermarkedStream = watermarker.AddWatermark(
                                                AppSettings.WatermarkSettings.WatermarkText,
                                                Color.FromArgb(128, 128, 128, 128),
                                                WatermarkPosition.BottomRight,
                                                15,
                                                AppSettings.WatermarkSettings.FontSize);
                        }

                        var response = await _imageStorageProvider.InsertAsync(name,
                            watermarkedStream != null ?
                                watermarkedStream.ToArray() :
                                stream.ToArray());

                        Logger.LogInformation("Image Upload: " + JsonConvert.SerializeObject(response));

                        if (response.IsSuccess)
                        {
                            string refPath = "/uploads/" + response.Item;
                            return Json(new { location = refPath });
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
            var s = _captcha.GenerateCaptchaImageFileStream(
                AppSettings.CaptchaSettings.ImageWidth,
                AppSettings.CaptchaSettings.ImageHeight,
                HttpContext.Session);
            return s;
        }

        // TODO: drop cache when avatar updated
        // [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 3600)]
        [Route("get-blogger-avatar")]
        public IActionResult GetBloggerAvatar()
        {
            if (!string.IsNullOrWhiteSpace(_blogConfig.BloggerAvatarBase64))
            {
                var avatarBytes = Convert.FromBase64String(_blogConfig.BloggerAvatarBase64);
                return File(avatarBytes, "image/png");
            }

            return File(@"images\avatar-placeholder.png", "image/png");
        }
    }
}