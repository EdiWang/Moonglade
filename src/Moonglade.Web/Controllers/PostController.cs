using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Data.DTO;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.IndexNow.Client;
using Moonglade.Webmention;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class PostController(
        ICacheAside cache,
        IConfiguration configuration,
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        IBlogConfig blogConfig,
        ScheduledPublishWakeUp wakeUp,
        ILogger<PostController> logger,
        CannonService cannonService) : BlogControllerBase(commandMediator)
{
    [HttpGet("list")]
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
    public async Task<IActionResult> CreateOrEdit(PostEditModel model)
    {
        try
        {
            if (model.ChangePublishDate &&
                model.PublishDate.HasValue &&
                model.PublishDate <= DateTime.UtcNow &&
                model.PublishDate.GetValueOrDefault().Year >= 1975)
            {
                model.PublishDate = model.PublishDate.Value;
            }

            if (model.PostStatus == PostStatus.Scheduled && model.ScheduledPublishTime.HasValue)
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
                    model.PostStatus = PostStatus.Published;
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
                await CommandMediator.SendAsync(new CreatePostCommand(model)) :
                await CommandMediator.SendAsync(new UpdatePostCommand(model.PostId, model));

            cache.Remove(BlogCachePartition.Post.ToString(), postEntity.RouteLink);

            var eventType = model.PostId == Guid.Empty ? EventType.PostCreated : EventType.PostUpdated;
            var operation = model.PostId == Guid.Empty ? "Create Post" : "Update Post";
            await LogActivityAsync(
                eventType,
                operation,
                model.Title,
                new { PostId = postEntity.Id, Slug = postEntity.RouteLink, PostStatus = model.PostStatus });

            if (model.PostStatus != PostStatus.Published)
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
            return Conflict("Error updating post.");
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
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new DeletePostCommand(postId, true));

        await LogActivityAsync(
            EventType.PostDeleted,
            "Delete Post (Move to Recycle Bin)",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/publish")]
    public async Task<IActionResult> Publish([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new PublishPostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        await LogActivityAsync(
            EventType.PostPublished,
            "Publish Post",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new UnpublishPostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        await LogActivityAsync(
            EventType.PostUnpublished,
            "Unpublish Post",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("keep-alive")]
    public IActionResult KeepAlive([MaxLength(16)] string nonce)
    {
        return Ok(new
        {
            ServerTime = DateTime.UtcNow,
            Nonce = nonce
        });
    }

    [HttpGet("meta")]
    public async Task<IActionResult> GetMeta([FromServices] IOptions<RequestLocalizationOptions> locOptions)
    {
        var ec = configuration.GetValue<EditorChoice>("Post:Editor");
        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());

        var response = new PostEditorMeta
        {
            EditorChoice = ec.ToString().ToLower(),
            DefaultAuthor = blogConfig.GeneralSettings.OwnerName,
            AbstractWords = blogConfig.ContentSettings.PostAbstractWords,
            Categories = cats.Select(c => new CategoryBrief
            {
                Id = c.Id,
                DisplayName = c.DisplayName
            }).ToList(),
            Languages = locOptions.Value.SupportedUICultures?
                .Select(c => new LanguageInfo
                {
                    Value = c.Name.ToLower(),
                    NativeName = c.NativeName
                }).ToList()
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPost([NotEmpty] Guid id)
    {
        var post = await queryMediator.QueryAsync(new GetPostByIdQuery(id));
        if (post == null) return NotFound();

        var tagStr = post.Tags
            .Select(p => p.DisplayName)
            .Aggregate(string.Empty, (current, item) => current + item + ",")
            .TrimEnd(',');

        var response = new PostEditDetail
        {
            PostId = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Author = post.Author,
            EditorContent = post.PostContent,
            PostStatus = post.PostStatus,
            EnableComment = post.CommentEnabled,
            FeedIncluded = post.IsFeedIncluded,
            Featured = post.IsFeatured,
            IsOutdated = post.IsOutdated,
            LanguageCode = post.ContentLanguageCode,
            ContentAbstract = post.ContentAbstract?.Replace("\u00A0\u2026", string.Empty),
            Keywords = post.Keywords,
            Tags = tagStr,
            PublishDate = post.PubDateUtc,
            ScheduledPublishTimeUtc = post.ScheduledPublishTimeUtc,
            LastModifiedUtc = post.LastModifiedUtc?.ToString("u"),
            SelectedCatIds = post.PostCategory.Select(pc => pc.CategoryId).ToArray()
        };

        return Ok(response);
    }

    [HttpGet("drafts")]
    public async Task<IActionResult> Drafts()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Draft));

        return Ok(new
        {
            Posts = posts
        });
    }
}