using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moonglade.Model;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    public partial class CommentController
    {
        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage(int page = 1)
        {
            const int pageSize = 20;
            var commentList = await _commentService.GetPagedCommentAsync(pageSize, page);
            var commentsAsIPagedList =
                new StaticPagedList<CommentListItem>(commentList, page, pageSize, _commentService.CountForApproved);
            return View(commentsAsIPagedList);
        }

        [Authorize]
        [HttpGet("pending-approval")]
        public IActionResult PendingApproval()
        {
            var list = _commentService.GetPendingApprovalComments();
            return View(list);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("approve-comments")]
        public async Task<IActionResult> ApproveComments(Guid[] commentIds)
        {
            var response = await _commentService.ApproveComments(commentIds);
            if (!response.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
            return Json(response);
        }

        // TODO: Obsolete this action
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("set-approval-status")]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId, bool isApproved)
        {
            if (isApproved)
            {
                var response = await _commentService.ApproveComments(new[] { commentId });
                if (response.IsSuccess)
                {
                    return Json(commentId);
                }

                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(response.ResponseCode);
            }

            return await Delete(commentId);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid commentId)
        {
            var response = await _commentService.DeleteComments(new[] { commentId });
            return response.IsSuccess ? Json(commentId) : Json(false);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("reply")]
        public IActionResult ReplyComment(Guid commentId, string replyContent, [FromServices] LinkGenerator linkGenerator)
        {
            var response = _commentService.NewReply(commentId, replyContent,
                HttpContext.Connection.RemoteIpAddress.ToString(), GetUserAgent());

            if (!response.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(response);
            }

            if (_blogConfig.EmailSettings.SendEmailOnCommentReply)
            {
                var postLink = GetPostUrl(linkGenerator, response.Item.PubDateUtc.GetValueOrDefault(), response.Item.Slug);
                Task.Run(async () => { await _notification.SendCommentReplyNotification(response.Item, postLink); });
            }

            return Json(response.Item);
        }
    }
}