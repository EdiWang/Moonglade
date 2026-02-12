using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ScheduleController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator
    ) : ControllerBase
{
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Scheduled));
        return Ok(posts);
    }

    [HttpPut("cancel/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new CancelScheduleCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        return NoContent();
    }

    [HttpPut("postpone/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Postpone([NotEmpty] Guid postId, [FromQuery][Range(1, 24)] int hours = 24)
    {
        await commandMediator.SendAsync(new PostponePostCommand(postId, hours));
        return NoContent();
    }
}
