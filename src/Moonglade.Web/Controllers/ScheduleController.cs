using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class ScheduleController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<ScheduleController> logger) : BlogControllerBase(commandMediator, logger)
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

        await LogActivityAsync(
            ActivityEvents.PostScheduleCancelled,
            $"Post #{postId}",
            ActivityLogDetails.Create(("PostId", postId)),
            targetId: postId.ToString());

        return NoContent();
    }

    [HttpPut("postpone/{postId:guid}")]
    public async Task<IActionResult> Postpone([NotEmpty] Guid postId, [FromQuery][Range(1, 24)] int hours = 24)
    {
        await CommandMediator.SendAsync(new PostponePostCommand(postId, hours));

        await LogActivityAsync(
            ActivityEvents.PostSchedulePostponed,
            $"Post #{postId}",
            ActivityLogDetails.Create(
                ("PostId", postId),
                ("Hours", hours)),
            targetId: postId.ToString());

        return NoContent();
    }
}
