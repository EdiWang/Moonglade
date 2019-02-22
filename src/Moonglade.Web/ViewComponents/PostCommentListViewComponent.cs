using System;
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

        public IViewComponentResult Invoke(string postId)
        {
            try
            {
                if (!Guid.TryParse(postId, out var id))
                {
                    Logger.LogWarning($"postId: {postId} is not a valid GUID");
                    return View("Error");
                }

                var comments = _commentService.GetApprovedCommentsOfPost(id);
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
