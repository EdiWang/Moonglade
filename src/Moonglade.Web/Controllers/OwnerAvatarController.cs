using LiteBus.Commands.Abstractions;
using Microsoft.Net.Http.Headers;
using Moonglade.ActivityLog;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

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
    [ResponseCache(Location = ResponseCacheLocation.None)]
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
            var contentType = bytes.AsSpan().StartsWith(jpegSignature) ? "image/jpeg" : "image/png";
            var entityTag = new EntityTagHeaderValue($"\"{Convert.ToHexString(SHA256.HashData(bytes))}\"");
            return File(bytes, contentType, lastModified: null, entityTag);
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
