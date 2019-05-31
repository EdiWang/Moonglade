using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class PostCommentListViewComponent : MoongladeViewComponent
    {
        private readonly CommentService _commentService;

        public PostCommentListViewComponent(
            ILogger<PostCommentListViewComponent> logger,
            IOptions<AppSettings> settings, CommentService commentService) : base(logger, settings)
        {
            _commentService = commentService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string postId)
        {
            try
            {
                if (!Guid.TryParse(postId, out var id))
                {
                    Logger.LogWarning($"postId: {postId} is not a valid GUID");
                    return View("Error");
                }

                var comments = await _commentService.GetSelectedCommentsOfPostAsync(id);
                return View(comments);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error reading comments for post id: {postId}");

                // should not block website
                return View("Error");
            }
        }
    }
}
