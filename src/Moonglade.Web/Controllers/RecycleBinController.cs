using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/post")]
[ApiController]
public class RecycleBinController(IMediator mediator) : ControllerBase
{
    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    [HttpPost("{postId:guid}/restore")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await mediator.Send(new RestorePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpDelete("{postId:guid}/destroy")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpDelete("recyclebin")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await mediator.Send(new EmptyRecycleBinCommand());
        return NoContent();
    }
}
