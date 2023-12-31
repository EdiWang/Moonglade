using Edi.ImageWatermark;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("image")]
public class ImageController(IBlogImageStorage imageStorage,
        ILogger<ImageController> logger,
        IBlogConfig blogConfig,
        IMemoryCache cache,
        IFileNameGenerator fileNameGen,
        IConfiguration configuration,
        IOptions<ImageStorageSettings> imageStorageSettings)
    : ControllerBase
{
    private readonly ImageStorageSettings _imageStorageSettings = imageStorageSettings.Value;

    [HttpGet(@"{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp))}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Image([MaxLength(256)] string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        if (filename.IndexOfAny(invalidChars) >= 0)
        {
            return BadRequest("invalid filename");
        }

        // Fallback method for legacy "/image/..." references (e.g. from third party websites)
        if (blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = blogConfig.ImageSettings.CDNEndpoint.CombineUrl(filename);
            return RedirectPermanent(imageUrl);
        }

        var image = await cache.GetOrCreateAsync(filename, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["CacheSlidingExpirationMinutes:Image"]));
            var imageInfo = await imageStorage.GetAsync(filename);
            return imageInfo;
        });

        if (null == image) return NotFound();

        return File(image.ImageBytes, image.ImageContentType);
    }

    [Authorize]
    [HttpPost, IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Image(IFormFile file, [FromQuery] bool skipWatermark = false)
    {
        if (file is null or { Length: <= 0 })
        {
            logger.LogError("file is null.");
            return BadRequest();
        }

        var name = Path.GetFileName(file.FileName);

        var ext = Path.GetExtension(name).ToLower();
        var allowedExts = _imageStorageSettings.AllowedExtensions;

        if (null == allowedExts || allowedExts.Length == 0)
        {
            throw new InvalidDataException($"{nameof(ImageStorageSettings.AllowedExtensions)} is empty.");
        }

        if (!allowedExts.Contains(ext))
        {
            logger.LogError($"Invalid file extension: {ext}");
            return BadRequest();
        }

        var primaryFileName = fileNameGen.GetFileName(name);
        var secondaryFieName = fileNameGen.GetFileName(name, "origin");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        stream.Position = 0;

        // Add watermark
        MemoryStream watermarkedStream = null;
        if (blogConfig.ImageSettings.IsWatermarkEnabled && !skipWatermark)
        {
            if (string.Compare(".gif", ext, StringComparison.OrdinalIgnoreCase) != 0)
            {
                using var watermarker = new ImageWatermarker(stream, ext, blogConfig.ImageSettings.WatermarkSkipPixel);

                watermarkedStream = watermarker.AddWatermark(
                    blogConfig.ImageSettings.WatermarkText,
                    Color.FromRgba(
                        128,
                        128,
                        128,
                        (byte)blogConfig.ImageSettings.WatermarkColorA),
                    WatermarkPosition.BottomRight,
                    15,
                    blogConfig.ImageSettings.WatermarkFontSize);
            }
            else
            {
                logger.LogInformation($"Skipped watermark for extension name: {ext}");
            }
        }

        var finalName = await imageStorage.InsertAsync(primaryFileName,
            watermarkedStream is not null ?
                watermarkedStream.ToArray() :
                stream.ToArray());

        if (blogConfig.ImageSettings.IsWatermarkEnabled && blogConfig.ImageSettings.KeepOriginImage || !skipWatermark)
        {
            var arr = stream.ToArray();
            _ = Task.Run(async () => await imageStorage.InsertAsync(secondaryFieName, arr));
        }

        logger.LogInformation($"Image '{primaryFileName}' uploaded.");
        var location = $"/image/{finalName}";
        var filename = location;

        return Ok(new
        {
            location,
            filename
        });
    }
}