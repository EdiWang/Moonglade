using Edi.ImageWatermark;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("image")]
public class ImageController : ControllerBase
{
    private readonly IBlogImageStorage _imageStorage;
    private readonly ILogger<ImageController> _logger;
    private readonly IBlogConfig _blogConfig;
    private readonly IMemoryCache _cache;
    private readonly IFileNameGenerator _fileNameGen;
    private readonly AppSettings _settings;
    private readonly ImageStorageSettings _imageStorageSettings;

    public ImageController(
        IBlogImageStorage imageStorage,
        ILogger<ImageController> logger,
        IBlogConfig blogConfig,
        IMemoryCache cache,
        IFileNameGenerator fileNameGen,
        IOptions<AppSettings> settings,
        IOptions<ImageStorageSettings> imageStorageSettings)
    {
        _imageStorage = imageStorage;
        _logger = logger;
        _blogConfig = blogConfig;
        _cache = cache;
        _fileNameGen = fileNameGen;
        _settings = settings.Value;
        _imageStorageSettings = imageStorageSettings.Value;
    }

    [HttpGet(@"{filename:regex((?!-)([[a-z0-9-]]+)\.(png|jpg|jpeg|gif|bmp))}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Image(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        if (filename.IndexOfAny(invalidChars) >= 0)
        {
            return BadRequest("invalid filename");
        }

        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(filename);
            return Redirect(imageUrl);
        }

        var image = await _cache.GetOrCreateAsync(filename, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Image"]);
            var imageInfo = await _imageStorage.GetAsync(filename);
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
        static bool IsValidColorValue(int colorValue)
        {
            return colorValue is >= 0 and <= 255;
        }

        if (file is null or { Length: <= 0 })
        {
            _logger.LogError("file is null.");
            return BadRequest();
        }

        var name = Path.GetFileName(file.FileName);

        var ext = Path.GetExtension(name).ToLower();
        var allowedExts = _imageStorageSettings.AllowedExtensions;

        if (null == allowedExts || !allowedExts.Any())
        {
            throw new InvalidDataException($"{nameof(ImageStorageSettings.AllowedExtensions)} is empty.");
        }

        if (!allowedExts.Contains(ext))
        {
            _logger.LogError($"Invalid file extension: {ext}");
            return BadRequest();
        }

        var primaryFileName = _fileNameGen.GetFileName(name);
        var secondaryFieName = _fileNameGen.GetFileName(name, "origin");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        stream.Position = 0;

        // Add watermark
        MemoryStream watermarkedStream = null;
        if (_blogConfig.ImageSettings.IsWatermarkEnabled && !skipWatermark)
        {
            if (null == _imageStorageSettings.Watermark.NoWatermarkExtensions
                || _imageStorageSettings.Watermark.NoWatermarkExtensions.All(
                    p => string.Compare(p, ext, StringComparison.OrdinalIgnoreCase) != 0))
            {
                using var watermarker = new ImageWatermarker(stream, ext, _imageStorageSettings.Watermark.WatermarkSkipPixel);

                // Get ARGB values
                var colors = _imageStorageSettings.Watermark.WatermarkARGB;
                if (colors.Length != 4)
                {
                    throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' must be an integer array with 4 items.");
                }

                if (colors.Any(c => !IsValidColorValue(c)))
                {
                    throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' values must all fall in range 0-255.");
                }

                watermarkedStream = watermarker.AddWatermark(
                    _blogConfig.ImageSettings.WatermarkText,
                    Color.FromRgba((byte)colors[1], (byte)colors[2], (byte)colors[3], (byte)colors[0]),
                    WatermarkPosition.BottomRight,
                    15,
                    _blogConfig.ImageSettings.WatermarkFontSize);
            }
            else
            {
                _logger.LogInformation($"Skipped watermark for extension name: {ext}");
            }
        }

        var finalName = await _imageStorage.InsertAsync(primaryFileName,
            watermarkedStream is not null ?
                watermarkedStream.ToArray() :
                stream.ToArray());

        if (_blogConfig.ImageSettings.KeepOriginImage)
        {
            var arr = stream.ToArray();
            _ = Task.Run(async () => await _imageStorage.InsertAsync(secondaryFieName, arr));
        }

        _logger.LogInformation($"Image '{primaryFileName}' uloaded.");
        var location = $"/image/{finalName}";
        var filename = location;

        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var url = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(finalName);
            location = url;
            filename = url;
        }

        return Ok(new
        {
            location,
            filename
        });
    }
}