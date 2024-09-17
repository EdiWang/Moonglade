using Moonglade.Core.PostFeature;
using Moonglade.IndexNow.Client;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;
using Moonglade.Webmention;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostController(
        IMediator mediator,
        IBlogConfig blogConfig,
        ITimeZoneResolver timeZoneResolver,
        ILogger<PostController> logger,
        CannonService cannonService) : ControllerBase
{
    [HttpPost("createoredit")]
    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrEdit(PostEditModel model)
    {
        try
        {
            if (!ModelState.IsValid) return Conflict(ModelState.CombineErrorMessages());

            var tzDate = timeZoneResolver.NowInTimeZone;
            if (model.ChangePublishDate &&
                model.PublishDate.HasValue &&
                model.PublishDate <= tzDate &&
                model.PublishDate.GetValueOrDefault().Year >= 1975)
            {
                model.PublishDate = timeZoneResolver.ToUtc(model.PublishDate.Value);
            }

            var postEntity = model.PostId == Guid.Empty ?
                await mediator.Send(new CreatePostCommand(model)) :
                await mediator.Send(new UpdatePostCommand(model.PostId, model));

            if (model.IsPublished)
            {
                logger.LogInformation($"Trying to Ping URL for post: {postEntity.Id}");

                var baseUri = new Uri(Helper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
                var link = new Uri(baseUri, $"post/{postEntity.RouteLink.ToLower()}");

                if (blogConfig.AdvancedSettings.EnablePingback)
                {
                    cannonService.FireAsync<IPingbackSender>(async sender => await sender.TrySendPingAsync(link.ToString(), postEntity.PostContent));
                }

                if (blogConfig.AdvancedSettings.EnableWebmention)
                {
                    cannonService.FireAsync<IWebmentionSender>(async sender => await sender.SendWebmentionAsync(link.ToString(), postEntity.PostContent));
                }

                cannonService.FireAsync<IIndexNowClient>(async sender => await sender.SendRequestAsync(link));
            }

            return Ok(new { PostId = postEntity.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Creating New Post.");
            return Conflict(ex.Message);
        }
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    [HttpPost("{postId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await mediator.Send(new RestorePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    [HttpDelete("{postId:guid}/recycle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId, true));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpDelete("{postId:guid}/destroy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFromRecycleBin([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpDelete("recyclebin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EmptyRecycleBin()
    {
        await mediator.Send(new PurgeRecycledCommand());
        return NoContent();
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("keep-alive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult KeepAlive([MaxLength(16)] string nonce)
    {
        return Ok(new
        {
            ServerTime = DateTime.UtcNow,
            Nonce = nonce
        });
    }
}