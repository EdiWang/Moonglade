using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Comments;

namespace Moonglade.Web.ViewComponents;

public class CommentListViewComponent : ViewComponent
{
    private readonly ILogger<CommentListViewComponent> _logger;
    private readonly IMediator _mediator;

    public CommentListViewComponent(
        ILogger<CommentListViewComponent> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid postId)
    {
        try
        {
            if (postId == Guid.Empty)
            {
                _logger.LogWarning($"postId: {postId} is not a valid GUID");
                throw new ArgumentOutOfRangeException(nameof(postId));
            }

            var comments = await _mediator.Send(new GetApprovedCommentsQuery(postId));
            return View(comments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error reading comments for post id: {postId}");
            return Content(e.Message);
        }
    }
}