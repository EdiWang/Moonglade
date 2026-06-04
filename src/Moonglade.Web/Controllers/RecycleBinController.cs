using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Page;
using Moonglade.Features.Post;

namespace Moonglade.Web.Controllers;

[Route("api/post")]
[TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
public class RecycleBinController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpGet("list/recyclebin")]
    public async Task<IActionResult> List()
    {
        var posts = await queryMediator.QueryAsync(new ListPostSegmentByStatusQuery(PostStatus.Deleted));
        var pages = await queryMediator.QueryAsync(new ListPageSegmentsQuery(DeletedOnly: true));

        return Ok(new
        {
            Posts = posts,
            Pages = pages
        });
    }

    [HttpPost("{postId:guid}/restore")]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new RestorePostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        await LogActivityAsync(
            EventType.PostRestored,
            "Restore Post",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [HttpDelete("{postId:guid}/destroy")]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await CommandMediator.SendAsync(new DeletePostCommand(postId));
        cache.Remove(BlogCachePartition.Post.ToString(), postId.ToString());

        await LogActivityAsync(
            EventType.PostPermanentlyDeleted,
            "Permanently Delete Post",
            $"Post #{postId}",
            new { PostId = postId });

        return NoContent();
    }

    [HttpPost("page/{pageId:guid}/restore")]
    public async Task<IActionResult> RestorePage([NotEmpty] Guid pageId)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(pageId));
        if (page == null) return NotFound();

        await CommandMediator.SendAsync(new RestorePageCommand(pageId));
        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug.ToLower());

        await LogActivityAsync(
            EventType.PageRestored,
            "Restore Page",
            page.Title,
            new { PageId = pageId, page.Slug });

        return NoContent();
    }

    [HttpDelete("page/{pageId:guid}/destroy")]
    public async Task<IActionResult> DeletePage([NotEmpty] Guid pageId)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(pageId));
        if (page == null) return NotFound();

        await CommandMediator.SendAsync(new DeletePageCommand(pageId));
        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug.ToLower());

        await LogActivityAsync(
            EventType.PagePermanentlyDeleted,
            "Permanently Delete Page",
            page.Title,
            new { PageId = pageId, page.Slug });

        return NoContent();
    }

    [HttpDelete("page/recyclebin")]
    public async Task<IActionResult> ClearPages()
    {
        var slugs = await CommandMediator.SendAsync(new EmptyPageRecycleBinCommand());

        foreach (var slug in slugs)
        {
            cache.Remove(BlogCachePartition.Page.ToString(), slug.ToLower());
        }

        await LogActivityAsync(
            EventType.PageRecycleBinCleared,
            "Clear Page Recycle Bin",
            "All deleted pages",
            new { Count = slugs.Length });

        return NoContent();
    }

    [HttpDelete("recyclebin")]
    public async Task<IActionResult> Clear()
    {
        var guids = await CommandMediator.SendAsync(new EmptyRecycleBinCommand());

        foreach (var guid in guids)
        {
            cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
        }

        await LogActivityAsync(
            EventType.RecycleBinCleared,
            "Clear Recycle Bin",
            "All deleted posts",
            new { Count = guids.Count() });

        return NoContent();
    }
}
