using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Options;
using Moonglade.Data.DTO;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Web.Commands;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class PostController(
    IConfiguration configuration,
    ICommandMediator commandMediator,
    IQueryMediator queryMediator,
    IBlogConfig blogConfig) : BlogControllerBase(commandMediator)
{
    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery][Range(1, int.MaxValue)] int pageIndex = 1, [FromQuery][Range(1, 100)] int pageSize = 4, [FromQuery] string searchTerm = null)
    {
        var offset = (pageIndex - 1) * pageSize;
        var (posts, totalRows) = await queryMediator.QueryAsync(new ListPostSegmentQuery(PostStatus.Published, offset, pageSize, searchTerm));

        return Ok(new PagedResult<PostSegment>(posts, pageIndex, pageSize, totalRows));
    }

    [HttpPost("createoredit")]
    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    public async Task<IActionResult> CreateOrEdit(PostEditModel model)
    {
        var result = await CommandMediator.SendAsync(new SavePostCommand(model, CreatePostOperationContext()));
        if (!result.Succeeded)
        {
            return Conflict(result.ErrorMessage);
        }

        return Ok(new { result.PostId });
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments =
    [
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription
    ])]
    [HttpDelete("{postId:guid}/recycle")]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new RecyclePostCommand(postId, CreatePostOperationContext()));

        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/publish")]
    public async Task<IActionResult> Publish([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new PublishPostWorkflowCommand(postId, CreatePostOperationContext()));

        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
    [HttpPut("{postId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new UnpublishPostWorkflowCommand(postId, CreatePostOperationContext()));

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
        var ec = configuration.GetValue<EditorChoice>("DefaultEditor");
        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());

        var response = new PostEditorMeta
        {
            EditorChoice = ec.ToString().ToLower(),
            DefaultAuthor = blogConfig.GeneralSettings.OwnerName,
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
            SelectedCatIds = post.PostCategory.Select(pc => pc.CategoryId).ToArray(),
            ContentType = post.ContentType
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

    private PostOperationContext CreatePostOperationContext()
    {
        return new(
            User.Identity?.Name ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            UrlHelper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
    }
}