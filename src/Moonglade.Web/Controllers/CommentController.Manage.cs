using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data.Entities;
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
                new StaticPagedList<Comment>(commentList, page, pageSize, _commentService.CountForApproved);
            return View(commentsAsIPagedList);
        }

        [Authorize]
        [Route("pending-approval")]
        public IActionResult PendingApproval()
        {
            var list = _commentService.GetPendingApprovalComments();
            return View(list);
        }

        [Authorize, HttpPost]
        [ValidateAntiForgeryToken]
        [Route("approve-comments")]
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
        [Authorize, HttpPost]
        [ValidateAntiForgeryToken]
        [Route("set-approval-status")]
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

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        [Route("delete")]
        public async Task<IActionResult> Delete(Guid commentId)
        {
            var response = await _commentService.DeleteComments(new[] { commentId });
            return response.IsSuccess ? Json(commentId) : Json(false);
        }

        [Authorize, HttpPost]
        [ValidateAntiForgeryToken]
        [Route("reply")]
        public IActionResult ReplyComment(Guid commentId, string replyContent)
        {
            var response = _commentService.NewReply(commentId, replyContent,
                HttpContext.Connection.RemoteIpAddress.ToString(), GetUserAgent());

            if (!response.IsSuccess) return Json(false);
            if (_blogConfig.EmailConfiguration.SendEmailOnCommentReply)
            {
                var postLink = GetPostUrl(_linkGenerator, response.Item.PubDateUtc.GetValueOrDefault(), response.Item.Slug);
                Task.Run(async () => { await _emailService.SendCommentReplyNotification(response.Item, postLink); });
            }

            return Json(response.Item);
        }
    }
}