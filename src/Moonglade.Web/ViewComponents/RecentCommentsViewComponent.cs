using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class RecentCommentsViewComponent : MoongladeViewComponent
    {
        private readonly CommentService _commentService;

        public RecentCommentsViewComponent(
            ILogger<RecentCommentsViewComponent> logger,
            MoongladeDbContext context,
            IOptions<AppSettings> settings, CommentService commentService) : base(logger, context, settings)
        {
            _commentService = commentService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.CompletedTask;
            var response = _commentService.GetRecentComments();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }
            // should not block website
            return View(new List<Comment>());
        }
    }
}
