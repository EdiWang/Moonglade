using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Features.Post;
using Moonglade.IndexNow.Client;
using Moonglade.Web.BackgroundServices;
using Moonglade.Web.Extensions;
using Moonglade.Webmention;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostController(
        ICacheAside cache,
        IConfiguration configuration,
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        IBlogConfig blogConfig,
        ScheduledPublishWakeUp wakeUp,
        ILogger<PostController> logger,
        CannonService cannonService) : ControllerBase
{
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 4, [FromQuery] string searchTerm = null)
    {
        var offset = (pageIndex - 1) * pageSize;
        var (posts, totalRows) = await queryMediator.QueryAsync(new ListPostSegmentQuery(PostStatus.Published, offset, pageSize, searchTerm));
        
        return Ok(new
        {
            Posts = posts,
            TotalRows = totalRows,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

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
            if (!ModelState.IsValid) return Conflict(ModelState.GetCombinedErrorMessage());

            if (model.ChangePublishDate &&
                model.PublishDate.HasValue &&
                model.PublishDate <= DateTime.UtcNow &&
                model.PublishDate.GetValueOrDefault().Year >= 1975)
            {
                model.PublishDate = model.PublishDate.Value;
            }

            if (model.PostStatus == PostStatusConstants.Scheduled && model.ScheduledPublishTime.HasValue)
            {
                if (string.IsNullOrWhiteSpace(model.ClientTimeZoneId))
                {
                    return Conflict("Client time zone ID is required for scheduled posts.");
                }

                var clientTimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.ClientTimeZoneId);
                var clientLocalTime = model.ScheduledPublishTime.Value;
                var clientUtcTime = TimeZoneInfo.ConvertTimeToUtc(clientLocalTime, clientTimeZone);

                model.ScheduledPublishTime = clientUtcTime;
                if (model.ScheduledPublishTime < DateTime.UtcNow)
                {
                    // return Conflict("Scheduled publish time must be in the future.");

                    // Instead of throwing error, just publish the post right away!
                    model.PostStatus = PostStatusConstants.Published;
                    model.ScheduledPublishTime = null;
                }
                else
                {
                    logger.LogInformation("Post scheduled for publish at {clientUtcTime} UTC.", clientUtcTime);

                    wakeUp.WakeUp();

                    logger.LogInformation("Scheduled publish wake-up triggered for post: {PostId}", model.PostId);
                }
            }

            var postEntity = model.PostId == Guid.Empty ?
                await commandMediator.SendAsync(new CreatePostCommand(model)) :
                await commandMediator.SendAsync(new UpdatePostCommand(model.PostId, model));

            cache.Remove(BlogCachePartition.Post.ToString(), postEntity.RouteLink);

            if (model.PostStatus != PostStatusConstants.Published)
            {
                return Ok(new { PostId = postEntity.Id });
            }

            logger.LogInformation("Trying to Ping URL for post: {Id}", postEntity.Id);

            var baseUri = new Uri(UrlHelper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
            var link = new Uri(baseUri, $"post/{postEntity.RouteLink.ToLower()}");

            NotifyExternalServices(postEntity.PostContent, link);
            ProcessIndexing(model.LastModifiedUtc, postEntity.LastModifiedUtc == postEntity.PubDateUtc, link);

            return Ok(new { PostId = postEntity.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating post.");
            return Conflict(ex.Message);
        }
    }

    private void NotifyExternalServices(string postContent, Uri link)
    {
        if (blogConfig.AdvancedSettings.EnableWebmention)
        {
            cannonService.FireAsync<IWebmentionSender>(async sender => await sender.SendWebmentionAsync(link.ToString(), postContent));
        }
    }

    private void ProcessIndexing(string lastModifiedUtc, bool isNewPublish, Uri link)
    {
        bool indexCoolDown = true;
        var minimalIntervalMinutes = int.Parse(configuration["IndexNow:MinimalIntervalMinutes"]!);
        if (!string.IsNullOrWhiteSpace(lastModifiedUtc))
        {
            var lastSavedInterval = DateTime.Parse(lastModifiedUtc) - DateTime.UtcNow;
            indexCoolDown = lastSavedInterval.TotalMinutes > minimalIntervalMinutes;
        }

        if (isNewPublish || indexCoolDown)
        {
            cannonService.FireAsync<IIndexNowClient>(async sender => await sender.SendRequestAsync(link));
        }
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
        await commandMediator.SendAsync(new DeletePostCommand(postId, true));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new PublishPostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new UnpublishPostCommand(postId));
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

    [HttpGet("drafts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Drafts()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Draft));
        
        return Ok(new
        {
            Posts = posts
        });
    }
}