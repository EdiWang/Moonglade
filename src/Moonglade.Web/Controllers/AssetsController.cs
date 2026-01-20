using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using SixLabors.ImageSharp;

namespace Moonglade.Web.Controllers;

[ApiController]
public class AssetsController(
    IEventMediator eventMediator,
    IQueryMediator queryMediator,
    IWebHostEnvironment env,
    ILogger<AssetsController> logger,
    ISiteIconBuilder siteIconBuilder) : ControllerBase
{
    [HttpGet("avatar")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Avatar(ICacheAside cache)
    {
        var bytes = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "avatar", async _ =>
        {
            logger.LogTrace("Avatar not on cache, getting new avatar image...");

            var data = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.AvatarBase64));
            if (string.IsNullOrWhiteSpace(data)) return null;

            var avatarBytes = Convert.FromBase64String(data);
            return avatarBytes;
        });

        if (null != bytes) return File(bytes, "image/png");

        var fallbackImageFile = Path.Join($"{env.WebRootPath}", "images", "default-avatar.png");
        return PhysicalFile(fallbackImageFile, "image/png");
    }

    [Authorize]
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "avatar"])]
    public async Task<IActionResult> Avatar([FromBody] string base64Img)
    {
        base64Img = base64Img.Trim();
        if (!Helper.TryParseBase64(base64Img, out var base64Chars))
        {
            logger.LogWarning("Bad base64 is used when setting avatar.");
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
            logger.LogError(e, "Invalid base64img Image");
            return Conflict(e.Message);
        }

        await eventMediator.PublishAsync(new SaveAssetEvent(AssetId.AvatarBase64, base64Img));
        logger.LogInformation("Avatar image updated successfully.");

        return Ok();
    }

    #region Site Icon

    [ResponseCache(Duration = 3600)]
    [HttpHead("/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
    [HttpGet("/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult SiteIcon(string filename)
    {
        var iconBytes = InMemoryIconGenerator.GetIcon(filename);
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
        var data = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.SiteIconBase64));
        var fallbackImageFile = Path.Join($"{env.WebRootPath}", "images", "siteicon-default.png");
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
            logger.LogError(e, $"Error {nameof(SiteIconOrigin)}(), Invalid Base64 string");
            return PhysicalFile(fallbackImageFile, "image/png");
        }
    }

    [Authorize]
    [HttpPost("siteicon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSiteIcon([FromBody] string base64Img)
    {
        base64Img = base64Img.Trim();
        if (!Helper.TryParseBase64(base64Img, out var base64Chars))
        {
            logger.LogWarning("Bad base64 is used when setting site icon.");
            return Conflict("Bad base64 data");
        }

        using var bmp = await Image.LoadAsync(new MemoryStream(base64Chars));
        if (bmp.Height != bmp.Width) return Conflict("image height must be equal to width");
        await eventMediator.PublishAsync(new SaveAssetEvent(AssetId.SiteIconBase64, base64Img));

        // Regenerate site icons immediately after update
        await siteIconBuilder.RegenerateSiteIcons(base64Img);

        logger.LogInformation("Site icon image updated successfully.");

        return NoContent();
    }

    #endregion
}