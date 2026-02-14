using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
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

        return Ok(new
        {
            Posts = posts
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
