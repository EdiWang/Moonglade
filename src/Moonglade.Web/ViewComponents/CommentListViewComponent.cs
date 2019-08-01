using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class CommentListViewComponent : MoongladeViewComponent
    {
        private readonly CommentService _commentService;

        public CommentListViewComponent(
            ILogger<CommentListViewComponent> logger,
            IOptions<AppSettings> settings, CommentService commentService) : base(logger, settings)
        {
            _commentService = commentService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid postId)
        {
            try
            {
                if (postId == Guid.Empty)
                {
                    Logger.LogWarning($"postId: {postId} is not a valid GUID");
                    return View("Error");
                }

                var comments = await _commentService.GetSelectedCommentsOfPostAsync(postId);
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
