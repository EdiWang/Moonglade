using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using SkiaSharp;

namespace Moonglade.Web.Controllers;

public class AssetsController(
    IEventMediator eventMediator,
    IQueryMediator queryMediator,
    IWebHostEnvironment env,
    ILogger<AssetsController> logger,
    SiteIconBuilder siteIconBuilder,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator, logger)
{
    #region Site Icon

    [AllowAnonymous]
    [ResponseCache(Duration = 3600)]
    [HttpHead("/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
    [HttpGet("/{filename:regex(^(favicon|android-icon|apple-icon).*(ico|png)$)}")]
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
            logger.LogError(e, "Error {Action}(), Invalid Base64 string", nameof(SiteIconOrigin));
            return PhysicalFile(fallbackImageFile, "image/png");
        }
    }

    [HttpPost("siteicon")]
    public async Task<IActionResult> UpdateSiteIcon([FromBody] string base64Img)
    {
        base64Img = base64Img.Trim();
        if (!Helper.TryParseBase64(base64Img, out var base64Chars))
        {
            logger.LogWarning("Bad base64 is used when setting site icon.");
            return Conflict("Bad base64 data");
        }

        using var bmp = SKBitmap.Decode(base64Chars);
        if (bmp is null) return Conflict("Invalid image data.");

        if (bmp.Height != bmp.Width) return Conflict("image height must be equal to width");
        await eventMediator.PublishAsync(new SaveAssetEvent(AssetId.SiteIconBase64, base64Img));

        // Regenerate site icons immediately after update
        await siteIconBuilder.RegenerateSiteIcons(base64Img);

        logger.LogInformation("Site icon image updated successfully.");

        await LogActivityAsync(
            ActivityEvents.SiteIconUpdated,
            "Site Icon",
            ActivityLogDetails.Create(("ImageSize", $"{bmp.Width}x{bmp.Height}")),
            targetId: AssetId.SiteIconBase64.ToString());

        return NoContent();
    }

    #endregion
}
