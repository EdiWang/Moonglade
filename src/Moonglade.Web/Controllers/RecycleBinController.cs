using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/post")]
[ApiController]
[TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
public class RecycleBinController(IMediator mediator) : ControllerBase
{
    [HttpPost("{postId:guid}/restore")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await mediator.Send(new RestorePostCommand(postId));
        return NoContent();
    }

    [HttpDelete("{postId:guid}/destroy")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId));
        return NoContent();
    }

    [HttpDelete("recyclebin")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await mediator.Send(new EmptyRecycleBinCommand());
        return NoContent();
    }
}
