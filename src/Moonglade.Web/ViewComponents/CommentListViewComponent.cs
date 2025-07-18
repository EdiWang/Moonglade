using LiteBus.Queries.Abstractions;

namespace Moonglade.Web.ViewComponents;

public class CommentListViewComponent(ILogger<CommentListViewComponent> logger, IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid postId)
    {
        try
        {
            if (postId == Guid.Empty)
            {
                logger.LogError($"postId: {postId} is not a valid GUID");
                throw new ArgumentOutOfRangeException(nameof(postId));
            }

            var comments = await queryMediator.QueryAsync(new GetApprovedCommentsQuery(postId));
            return View(comments);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reading comments for post id: {postId}");
            return Content("ERROR");
        }
    }
}