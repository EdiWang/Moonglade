using LiteBus.Commands.Abstractions;
using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ScheduleController(
    ICacheAside cache,
    ICommandMediator commandMediator
    ) : ControllerBase
{
    [HttpPut("{postId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new CancelScheduleCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        return NoContent();
    }

    [HttpPut("{postId:guid}/postpone")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Postpone([NotEmpty] Guid postId, [FromQuery][Range(1, 24)] int hours = 24)
    {
        await commandMediator.SendAsync(new PostponePostCommand(postId, hours));
        return NoContent();
    }
}
