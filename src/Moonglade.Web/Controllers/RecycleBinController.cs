using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Page;
using Moonglade.Features.Post;

namespace Moonglade.Web.Controllers;

[Route("api/post")]
public class RecycleBinController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<RecycleBinController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet("list/recyclebin")]
    public async Task<IActionResult> List()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Deleted));
        var pages = await queryMediator.QueryAsync(new ListPageSegmentsQuery(DeletedOnly: true));

        return Ok(new RecycleBinListResponse(posts, pages));
    }

    [HttpPost("{postId:guid}/restore")]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new RestorePostCommand(postId));

        await LogActivityAsync(
            ActivityEvents.PostRestored,
            $"Post #{postId}",
            ActivityLogDetails.Create(("PostId", postId)),
            targetId: postId.ToString());

        return NoContent();
    }

    [HttpDelete("{postId:guid}/destroy")]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new DeletePostCommand(postId));

        await LogActivityAsync(
            ActivityEvents.PostPermanentlyDeleted,
            $"Post #{postId}",
            ActivityLogDetails.Create(("PostId", postId)),
            targetId: postId.ToString());

        return NoContent();
    }

    [HttpPost("page/{pageId:guid}/restore")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    public async Task<IActionResult> RestorePage([NotEmpty] Guid pageId)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(pageId));
        if (page == null) return NotFound();

        await CommandMediator.SendAsync(new RestorePageCommand(pageId));
        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug.ToLower());

        await LogActivityAsync(
            ActivityEvents.PageRestored,
            page.Title,
            ActivityLogDetails.Create(
                ("PageId", pageId),
                ("Slug", page.Slug)),
            targetId: pageId.ToString());

        return NoContent();
    }

    [HttpDelete("page/{pageId:guid}/destroy")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    public async Task<IActionResult> DeletePage([NotEmpty] Guid pageId)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(pageId));
        if (page == null) return NotFound();

        await CommandMediator.SendAsync(new DeletePageCommand(pageId));
        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug.ToLower());

        await LogActivityAsync(
            ActivityEvents.PagePermanentlyDeleted,
            page.Title,
            ActivityLogDetails.Create(
                ("PageId", pageId),
                ("Slug", page.Slug)),
            targetId: pageId.ToString());

        return NoContent();
    }

    [HttpDelete("page/recyclebin")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    public async Task<IActionResult> ClearPages()
    {
        var slugs = await CommandMediator.SendAsync(new EmptyPageRecycleBinCommand());

        foreach (var slug in slugs)
        {
            cache.Remove(BlogCachePartition.Page.ToString(), slug.ToLower());
        }

        await LogActivityAsync(
            ActivityEvents.PageRecycleBinCleared,
            "All deleted pages",
            ActivityLogDetails.Create(("Count", slugs.Length)));

        return NoContent();
    }

    [HttpDelete("recyclebin")]
    public async Task<IActionResult> Clear()
    {
        var guids = await CommandMediator.SendAsync(new EmptyRecycleBinCommand());

        await LogActivityAsync(
            ActivityEvents.RecycleBinCleared,
            "All deleted posts",
            ActivityLogDetails.Create(("Count", guids.Count())));

        return NoContent();
    }
}

file sealed record RecycleBinListResponse(
    IReadOnlyList<Moonglade.Data.DTO.PostSegment> Posts,
    IReadOnlyList<Moonglade.Data.DTO.PageSegment> Pages);
