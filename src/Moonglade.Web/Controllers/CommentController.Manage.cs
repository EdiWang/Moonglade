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
        public IActionResult Manage(int page = 1)
        {
            const int pageSize = 20;
            var commentList = _commentService.GetPagedComment(pageSize, page);
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
        [Route("set-approval-status")]
        public IActionResult SetApprovalStatus(Guid commentId, bool isApproved)
        {
            var response = _commentService.SetApprovalStatus(commentId, isApproved);
            if (response.IsSuccess)
            {
                return Json(commentId);
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Json(response.ResponseCode);
        }

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        [Route("delete")]
        public IActionResult Delete(Guid commentId)
        {
            var response = _commentService.Delete(commentId);
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