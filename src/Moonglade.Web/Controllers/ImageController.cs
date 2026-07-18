using Edi.ImageWatermark;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using SkiaSharp;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("image")]
public class ImageController(
        IBlogImageStorage imageStorage,
        ILogger<ImageController> logger,
        IBlogConfig blogConfig,
        IMemoryCache cache,
        IFileNameGenerator fileNameGen,
        IOptions<ImageStorageSettings> imageStorageSettings,
        CannonService cannonService,
        ICommandMediator commandMediator)
    : BlogControllerBase(commandMediator)
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private const string ImageCacheKeyPrefix = "image-meta:";

    private readonly ImageStorageSettings _imageStorageSettings = imageStorageSettings.Value;

    [AllowAnonymous]
    [HttpGet(@"{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp|webp|svg))}")]
    [HttpHead(@"{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp|webp|svg))}")]
    public async Task<IActionResult> Image([MaxLength(256)] string filename, CancellationToken cancellationToken = default)
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

        var image = await GetImageInfoAsync(filename, cancellationToken);

        if (image == null) return NotFound();

        var entityTag = TryCreateEntityTag(image.EntityTag);
        ApplyCacheHeaders(image, entityTag);

        if (IsNotModified(image, entityTag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var imageStream = await imageStorage.OpenReadAsync(filename);
        if (imageStream == null)
        {
            cache.Remove(GetImageCacheKey(filename));
            return NotFound();
        }

        return File(imageStream, image.ImageContentType, image.LastModifiedUtc, entityTag, enableRangeProcessing: true);
    }

    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> Image([Required] IFormFile file, [FromQuery] bool skipWatermark = false)
    {
        if (file.Length <= 0)
        {
            return BadRequest("Image file is empty.");
        }

        if (file.Length > MaxUploadBytes)
        {
            logger.LogWarning("Image upload rejected because file size {FileSize} exceeds limit {MaxUploadBytes}.", file.Length, MaxUploadBytes);
            return BadRequest("Image file size cannot exceed 5 MB.");
        }

        var name = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(name).ToLower();
        string[] allowedExts = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"];

        if (!allowedExts.Contains(ext))
        {
            logger.LogWarning("Invalid file extension: {Extension}", ext);
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

        logger.LogInformation("Image '{FileName}' uploaded.", primaryFileName);

        await LogActivityAsync(
            EventType.ImageUploaded,
            "Upload Image",
            finalName,
            new { FileName = finalName, FileSize = file.Length, SkipWatermark = skipWatermark });

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
            logger.LogInformation("Skipped watermark for extension name: {Extension}", ext);
            return null;
        }

        using var watermarker = new ImageWatermarker(stream, blogConfig.ImageSettings.WatermarkSkipPixel);
        return watermarker.AddWatermark(
            blogConfig.ImageSettings.WatermarkText,
            new SKColor(128, 128, 128, (byte)blogConfig.ImageSettings.WatermarkColorA),
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
            await storage.InsertSecondaryAsync(fileName, originalImageData));
    }

    private async Task<ImageInfo> GetImageInfoAsync(string filename, CancellationToken cancellationToken)
    {
        if (_imageStorageSettings.CacheMinutes <= 0)
        {
            return await imageStorage.GetInfoAsync(filename);
        }

        return await cache.GetOrCreateAsync(GetImageCacheKey(filename), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(_imageStorageSettings.CacheMinutes);
            return await imageStorage.GetInfoAsync(filename);
        });
    }

    private static string GetImageCacheKey(string filename) => $"{ImageCacheKeyPrefix}{filename}";

    private void ApplyCacheHeaders(ImageInfo image, EntityTagHeaderValue entityTag)
    {
        var typedHeaders = Response.GetTypedHeaders();
        typedHeaders.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(Math.Max(0, _imageStorageSettings.CacheMinutes))
        };
        typedHeaders.ETag = entityTag;
        typedHeaders.LastModified = image.LastModifiedUtc;
    }

    private bool IsNotModified(ImageInfo image, EntityTagHeaderValue entityTag)
    {
        var requestHeaders = Request.GetTypedHeaders();

        if (requestHeaders.IfNoneMatch is { Count: > 0 })
        {
            if (entityTag == null) return false;

            return requestHeaders.IfNoneMatch.Any(tag =>
                string.Equals(tag.ToString(), "*", StringComparison.Ordinal) ||
                string.Equals(tag.ToString(), entityTag.ToString(), StringComparison.Ordinal));
        }

        if (requestHeaders.IfModifiedSince.HasValue && image.LastModifiedUtc.HasValue)
        {
            return TruncateToSeconds(image.LastModifiedUtc.Value) <= requestHeaders.IfModifiedSince.Value;
        }

        return false;
    }

    private static EntityTagHeaderValue TryCreateEntityTag(string entityTag)
    {
        if (string.IsNullOrWhiteSpace(entityTag))
        {
            return null;
        }

        try
        {
            return EntityTagHeaderValue.Parse(entityTag);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static DateTimeOffset TruncateToSeconds(DateTimeOffset value)
    {
        var utcValue = value.ToUniversalTime();
        return new DateTimeOffset(utcValue.Ticks - utcValue.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
    }
}
