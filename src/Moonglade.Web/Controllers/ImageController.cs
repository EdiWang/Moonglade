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
    private readonly IFileNameGenerator _fileNameGenerator;
    private readonly AppSettings _settings;
    private readonly ImageStorageSettings _imageStorageSettings;

    public ImageController(
        IBlogImageStorage imageStorage,
        ILogger<ImageController> logger,
        IBlogConfig blogConfig,
        IMemoryCache cache,
        IFileNameGenerator fileNameGenerator,
        IOptions<AppSettings> settings,
        IOptions<ImageStorageSettings> imageStorageSettings)
    {
        _imageStorage = imageStorage;
        _logger = logger;
        _blogConfig = blogConfig;
        _cache = cache;
        _fileNameGenerator = fileNameGenerator;
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

        _logger.LogTrace($"Requesting image file {filename}");

        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(filename);
            return Redirect(imageUrl);
        }

        var image = await _cache.GetOrCreateAsync(filename, async entry =>
        {
            _logger.LogTrace($"Image file {filename} not on cache, fetching image...");

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
        var allowedExtensions = _imageStorageSettings.AllowedExtensions;

        if (null == allowedExtensions || !allowedExtensions.Any())
        {
            throw new InvalidDataException($"{nameof(ImageStorageSettings.AllowedExtensions)} is empty.");
        }

        if (!allowedExtensions.Contains(ext))
        {
            _logger.LogError($"Invalid file extension: {ext}");
            return BadRequest();
        }

        var primaryFileName = _fileNameGenerator.GetFileName(name);
        var secondaryFieName = _fileNameGenerator.GetFileName(name, "origin");

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
                var colorArray = _imageStorageSettings.Watermark.WatermarkARGB;
                if (colorArray.Length != 4)
                {
                    throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' must be an integer array with 4 items.");
                }

                if (colorArray.Any(c => !IsValidColorValue(c)))
                {
                    throw new InvalidDataException($"'{nameof(_imageStorageSettings.Watermark.WatermarkARGB)}' values must all fall in range 0-255.");
                }

                watermarkedStream = watermarker.AddWatermark(
                    _blogConfig.ImageSettings.WatermarkText,
                    Color.FromRgba((byte)colorArray[1], (byte)colorArray[2], (byte)colorArray[3], (byte)colorArray[0]),
                    WatermarkPosition.BottomRight,
                    15,
                    _blogConfig.ImageSettings.WatermarkFontSize);
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

        if (_blogConfig.ImageSettings.KeepOriginImage)
        {
            var arr = stream.ToArray();
            _ = Task.Run(async () => await _imageStorage.InsertAsync(secondaryFieName, arr));
        }

        _logger.LogInformation($"Image '{primaryFileName}' uloaded.");

        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(finalFileName);

            return Ok(new
            {
                location = imageUrl,
                filename = imageUrl
            });
        }
        else
        {
            return Ok(new
            {
                location = $"/image/{finalFileName}",
                filename = finalFileName
            });
        }
    }
}