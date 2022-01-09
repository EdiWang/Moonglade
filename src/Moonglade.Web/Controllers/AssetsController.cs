using Moonglade.Caching.Filters;
using SixLabors.ImageSharp;

namespace Moonglade.Web.Controllers;

[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        ILogger<AssetsController> logger,
        IMediator mediator,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    [HttpGet("avatar")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Avatar([FromServices] IBlogCache cache)
    {
        var fallbackImageFile = Path.Join($"{_env.WebRootPath}", "images", "default-avatar.png");

        try
        {
            var bytes = await cache.GetOrCreateAsync(CacheDivision.General, "avatar", async _ =>
            {
                _logger.LogTrace("Avatar not on cache, getting new avatar image...");

                var data = await _mediator.Send(new GetAssetDataQuery(AssetId.AvatarBase64));
                if (string.IsNullOrWhiteSpace(data)) return null;

                var avatarBytes = Convert.FromBase64String(data);
                return avatarBytes;
            });

            if (null == bytes)
            {
                return PhysicalFile(fallbackImageFile, "image/png");
            }

            return File(bytes, "image/png");
        }
        catch (FormatException e)
        {
            _logger.LogError($"Error {nameof(Avatar)}(), Invalid Base64 string", e);
            return PhysicalFile(fallbackImageFile, "image/png");
        }
    }

    [Authorize]
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "avatar" })]
    public async Task<IActionResult> Avatar([FromForm] string base64Img)
    {
        base64Img = base64Img.Trim();
        if (!Helper.TryParseBase64(base64Img, out var base64Chars))
        {
            _logger.LogWarning("Bad base64 is used when setting avatar.");
            return Conflict("Bad base64 data");
        }

        try
        {
            using var bmp = await Image.LoadAsync(new MemoryStream(base64Chars));
            if (bmp.Height != bmp.Width || bmp.Height + bmp.Width != 600)
            {
                return Conflict("Image size must be 300x300.");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Invalid base64img Image", e);
            return Conflict(e.Message);
        }

        await _mediator.Publish(new SaveAssetCommand(AssetId.AvatarBase64, base64Img));

        return Ok();
    }

    #region Site Icon

    [ResponseCache(Duration = 3600)]
    [HttpHead(@"/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
    [HttpGet(@"/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult SiteIcon(string filename)
    {
        var iconBytes = MemoryStreamIconGenerator.GetIcon(filename);
        if (iconBytes is null) return NotFound();

        var contentType = "image/png";
        var ext = Path.GetExtension(filename);
        contentType = ext switch
        {
            ".png" => "image/png",
            ".ico" => "image/x-icon",
            _ => contentType
        };
        return File(iconBytes, contentType);
    }

    [Authorize]
    [HttpGet("siteicon")]
    public async Task<IActionResult> SiteIconOrigin()
    {
        var data = await _mediator.Send(new GetAssetDataQuery(AssetId.SiteIconBase64));
        var fallbackImageFile = Path.Join($"{_env.WebRootPath}", "images", "siteicon-default.png");
        if (string.IsNullOrWhiteSpace(data))
        {
            return PhysicalFile(fallbackImageFile, "image/png");
        }

        try
        {
            var siteIconBytes = Convert.FromBase64String(data);
            return File(siteIconBytes, "image/png");
        }
        catch (FormatException e)
        {
            _logger.LogError($"Error {nameof(SiteIconOrigin)}(), Invalid Base64 string", e);
            return PhysicalFile(fallbackImageFile, "image/png");
        }
    }

    [Authorize]
    [HttpPost("siteicon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSiteIcon([FromForm] string base64Img)
    {
        base64Img = base64Img.Trim();
        if (!Helper.TryParseBase64(base64Img, out var base64Chars))
        {
            _logger.LogWarning("Bad base64 is used when setting site icon.");
            return Conflict("Bad base64 data");
        }

        using var bmp = await Image.LoadAsync(new MemoryStream(base64Chars));
        if (bmp.Height != bmp.Width) return Conflict("image height must be equal to width");
        await _mediator.Publish(new SaveAssetCommand(AssetId.SiteIconBase64, base64Img));

        return NoContent();
    }

    #endregion
}