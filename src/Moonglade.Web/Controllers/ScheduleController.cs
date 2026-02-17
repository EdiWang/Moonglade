using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class ScheduleController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator
    ) : BlogControllerBase(commandMediator)
{
    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Scheduled));
        return Ok(posts);
    }

    [HttpPut("cancel/{postId:guid}")]
    public async Task<IActionResult> Cancel([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new CancelScheduleCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        await LogActivityAsync(
            EventType.PostScheduleCancelled,
            "Cancel Post Schedule",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [HttpPut("postpone/{postId:guid}")]
    public async Task<IActionResult> Postpone([NotEmpty] Guid postId, [FromQuery][Range(1, 24)] int hours = 24)
    {
        await CommandMediator.SendAsync(new PostponePostCommand(postId, hours));

        await LogActivityAsync(
            EventType.PostSchedulePostponed,
            "Postpone Post Schedule",
            $"Post #{postId}",
            new { PostId = postId, Hours = hours });

        return NoContent();
    }
}
