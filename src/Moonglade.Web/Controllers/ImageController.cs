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
    private readonly IConfiguration _configuration;
    private readonly ImageStorageSettings _imageStorageSettings;

    public ImageController(
        IBlogImageStorage imageStorage,
        ILogger<ImageController> logger,
        IBlogConfig blogConfig,
        IMemoryCache cache,
        IFileNameGenerator fileNameGen,
        IConfiguration configuration,
        IOptions<ImageStorageSettings> imageStorageSettings)
    {
        _imageStorage = imageStorage;
        _logger = logger;
        _blogConfig = blogConfig;
        _cache = cache;
        _fileNameGen = fileNameGen;
        _configuration = configuration;
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

        // Fallback method for legacy "/image/..." references (e.g. from third party websites)
        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var imageUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(filename);
            return Redirect(imageUrl);
        }

        var image = await _cache.GetOrCreateAsync(filename, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(_configuration["CacheSlidingExpirationMinutes:Image"]));
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
            if (string.Compare(".gif", ext, StringComparison.OrdinalIgnoreCase) != 0)
            {
                using var watermarker = new ImageWatermarker(stream, ext, _blogConfig.ImageSettings.WatermarkSkipPixel);

                watermarkedStream = watermarker.AddWatermark(
                    _blogConfig.ImageSettings.WatermarkText,
                    Color.FromRgba(
                        (byte)_blogConfig.ImageSettings.WatermarkColorR,
                        (byte)_blogConfig.ImageSettings.WatermarkColorG,
                        (byte)_blogConfig.ImageSettings.WatermarkColorB,
                        (byte)_blogConfig.ImageSettings.WatermarkColorA),
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

        if (_blogConfig.ImageSettings.IsWatermarkEnabled && _blogConfig.ImageSettings.KeepOriginImage || !skipWatermark)
        {
            var arr = stream.ToArray();
            _ = Task.Run(async () => await _imageStorage.InsertAsync(secondaryFieName, arr));
        }

        _logger.LogInformation($"Image '{primaryFileName}' uploaded.");
        var location = $"/image/{finalName}";
        var filename = location;

        return Ok(new
        {
            location,
            filename
        });
    }
}