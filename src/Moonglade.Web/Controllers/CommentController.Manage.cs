using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Model;
using X.PagedList;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Web.Controllers
{
    public partial class CommentController
    {
        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage(int page = 1)
        {
            const int pageSize = 10;
            var commentList = await _commentService.GetPagedCommentAsync(pageSize, page);
            var commentsAsIPagedList =
                new StaticPagedList<CommentListItem>(commentList.Item, page, pageSize, _commentService.CountComments());
            return View(commentsAsIPagedList);
        }

        [Authorize]
        [HttpPost("set-approval-status")]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId)
        {
            var response = await _commentService.ToggleCommentApprovalStatus(new[] { commentId });
            if (response.IsSuccess)
            {
                Logger.LogInformation($"User '{User.Identity.Name}' updated approval status of comment id '{commentId}'");
                return Json(commentId);
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Json(response.ResponseCode);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid commentId, [FromServices] IMoongladeAudit moongladeAudit)
        {
            var response = await _commentService.DeleteComments(new[] { commentId });
            Logger.LogInformation($"User '{User.Identity.Name}' deleting comment id '{commentId}'");
            
            if (response.IsSuccess)
            {
                await moongladeAudit.AddAuditEntry(EventType.Content, EventId.CommentDeleted, $"Comment '{commentId}' deleted.");
                return Json(commentId);
            }
            return Json(false);
        }

        [Authorize]
        [HttpPost("reply")]
        public async Task<IActionResult> ReplyComment(
            Guid commentId, string replyContent,
            [FromServices] LinkGenerator linkGenerator, [FromServices] IMoongladeAudit moongladeAudit)
        {
            var response = _commentService.AddReply(commentId, replyContent,
                HttpContext.Connection.RemoteIpAddress.ToString(), GetUserAgent());

            if (!response.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(response);
            }

            if (_blogConfig.EmailSettings.SendEmailOnCommentReply)
            {
                var postLink = GetPostUrl(linkGenerator, response.Item.PubDateUtc, response.Item.Slug);
                _ = Task.Run(async () => { await _notificationClient.SendCommentReplyNotificationAsync(response.Item, postLink); });
            }

            var logMessage = $"User '{User.Identity.Name}' replied comment id '{commentId}'";
            Logger.LogInformation(logMessage);
            await moongladeAudit.AddAuditEntry(EventType.Content, EventId.CommentReplied, logMessage);

            return Json(response.Item);
        }
    }
}