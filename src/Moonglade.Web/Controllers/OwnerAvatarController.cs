using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

public class OwnerAvatarController(
    OwnerAvatarStore avatars,
    IWebHostEnvironment env,
    ICacheAside cache,
    ICommandMediator commandMediator,
    ILogger<OwnerAvatarController> logger) : BlogControllerBase(commandMediator, logger)
{
    private const string ImageCacheKey = "avatar";

    [AllowAnonymous]
    [HttpGet("avatar")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Avatar(CancellationToken ct)
    {
        var bytes = await cache.GetOrCreateAsync(nameof(BlogCachePartition.Account), ImageCacheKey, async () =>
        {
            var data = await avatars.ReadImageAsync(ct);
            return string.IsNullOrWhiteSpace(data) ? null : Convert.FromBase64String(data);
        });

        if (bytes is not null)
        {
            ReadOnlySpan<byte> jpegSignature = [0xFF, 0xD8, 0xFF];
            return File(bytes, bytes.AsSpan().StartsWith(jpegSignature) ? "image/jpeg" : "image/png");
        }

        return PhysicalFile(Path.Join(env.WebRootPath, "images", "default-avatar.png"), "image/png");
    }

    [HttpPost("avatar")]
    [HttpPost("api/account/avatar")]
    public async Task<IActionResult> Upload([FromBody] string base64Image, CancellationToken ct)
    {
        try
        {
            await avatars.SetImageAsync(base64Image, ct);
        }
        catch (ValidationException exception)
        {
            return Conflict(exception.Message);
        }

        cache.Remove(nameof(BlogCachePartition.Account), ImageCacheKey);
        await LogActivityAsync(ActivityEvents.AvatarUpdated, "Avatar Image");
        return Ok();
    }

    [HttpPut("api/account/avatar/gravatar")]
    public async Task<IActionResult> UpdateGravatar([FromBody] bool enabled, CancellationToken ct)
    {
        if (await avatars.SetGravatarAsync(enabled, ct))
            await LogActivityAsync(ActivityEvents.AvatarUpdated, "Gravatar");
        return NoContent();
    }
}
