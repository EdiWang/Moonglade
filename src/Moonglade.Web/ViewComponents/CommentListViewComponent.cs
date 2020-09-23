using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class CommentListViewComponent : ViewComponent
    {
        private readonly ILogger<CommentListViewComponent> _logger;
        private readonly CommentService _commentService;

        public CommentListViewComponent(
            ILogger<CommentListViewComponent> logger, CommentService commentService)
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
                    return View("Error");
                }

                var comments = await _commentService.GetSelectedCommentsOfPostAsync(postId);
                return View(comments);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error reading comments for post id: {postId}");

                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
