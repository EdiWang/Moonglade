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
        IOptions<ImageStorageSettings> imageStorageSettings,
        CannonService cannonService)
    : ControllerBase
{
    private readonly ImageStorageSettings _imageStorageSettings = imageStorageSettings.Value;

    [HttpGet(@"{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp|webp|svg))}")]
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

        // Prevent access to origin images
        if (filename.Contains("-origin.", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning($"Attempt to access origin image: `{filename}` is blocked. Client IP: {HttpContext.Connection.RemoteIpAddress}");
            return Forbid();
        }

        // Fallback method for legacy "/image/..." references (e.g. from third party websites)
        if (blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = blogConfig.ImageSettings.CDNEndpoint.CombineUrl(filename);
            return RedirectPermanent(imageUrl);
        }

        var image = await cache.GetOrCreateAsync(filename, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(_imageStorageSettings.CacheMinutes);
            var imageInfo = await imageStorage.GetAsync(filename);
            return imageInfo;
        });

        if (null == image) return NotFound();

        return File(image.ImageBytes, image.ImageContentType);
    }

    [Authorize]
    [ReadonlyMode]
    [HttpPost, IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Image([Required] IFormFile file, [FromQuery] bool skipWatermark = false)
    {
        var name = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(name).ToLower();
        string[] allowedExts = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"];

        if (!allowedExts.Contains(ext))
        {
            logger.LogError($"Invalid file extension: {ext}");
            return BadRequest();
        }

        var primaryFileName = fileNameGen.GetFileName(name);
        var secondaryFileName = fileNameGen.GetFileName(name, "origin");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var watermarkedStream = AddWatermarkIfNeeded(stream, ext, skipWatermark);
        var finalName = await imageStorage.InsertAsync(primaryFileName, watermarkedStream ?? stream.ToArray());

        if (ShouldKeepOriginal(skipWatermark))
        {
            StoreOriginalImageAsync(secondaryFileName, stream);
        }

        logger.LogInformation($"Image '{primaryFileName}' uploaded.");

        return Ok(new
        {
            location = $"/image/{finalName}",
            filename = $"/image/{finalName}"
        });
    }

    private byte[] AddWatermarkIfNeeded(MemoryStream stream, string ext, bool skipWatermark)
    {
        if (!blogConfig.ImageSettings.IsWatermarkEnabled || skipWatermark) return null;

        if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation($"Skipped watermark for extension name: {ext}");
            return null;
        }

        using var watermarker = new ImageWatermarker(stream, ext, blogConfig.ImageSettings.WatermarkSkipPixel);
        return watermarker.AddWatermark(
            blogConfig.ImageSettings.WatermarkText,
            Color.FromRgba(128, 128, 128, (byte)blogConfig.ImageSettings.WatermarkColorA),
            WatermarkPosition.BottomRight,
            15,
            blogConfig.ImageSettings.WatermarkFontSize)?.ToArray();
    }

    private bool ShouldKeepOriginal(bool skipWatermark)
    {
        return blogConfig.ImageSettings.IsWatermarkEnabled &&
               (blogConfig.ImageSettings.KeepOriginImage || !skipWatermark);
    }

    private void StoreOriginalImageAsync(string fileName, MemoryStream stream)
    {
        var originalImageData = stream.ToArray();
        cannonService.FireAsync<IBlogImageStorage>(async storage =>
            await storage.InsertAsync(fileName, originalImageData));
    }
}