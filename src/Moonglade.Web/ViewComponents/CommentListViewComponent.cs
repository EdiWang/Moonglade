using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Comments;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.ViewComponents
{
    public class CommentListViewComponent : ViewComponent
    {
        private readonly ILogger<CommentListViewComponent> _logger;
        private readonly ICommentService _commentService;

        public CommentListViewComponent(
            ILogger<CommentListViewComponent> logger, ICommentService commentService)
        {
            _logger = logger;
            _commentService = commentService;
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

                var comments = await _commentService.GetApprovedCommentsAsync(postId);
                return View(comments);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error reading comments for post id: {postId}");
                return Content(e.Message);
            }
        }
    }
}
