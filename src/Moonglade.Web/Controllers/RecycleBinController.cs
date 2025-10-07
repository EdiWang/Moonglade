using LiteBus.Commands.Abstractions;
using Moonglade.Features.Post;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/post")]
[ApiController]
[TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
public class RecycleBinController(ICacheAside cache, ICommandMediator commandMediator) : ControllerBase
{
    [HttpPost("{postId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new RestorePostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        return NoContent();
    }

    [HttpDelete("{postId:guid}/destroy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new DeletePostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        return NoContent();
    }

    [HttpDelete("recyclebin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        var guids = await commandMediator.SendAsync(new EmptyRecycleBinCommand());

        foreach (var guid in guids)
        {
            cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
        }

        return NoContent();
    }
}
