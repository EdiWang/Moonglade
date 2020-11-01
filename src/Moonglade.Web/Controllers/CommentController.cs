using System;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Model;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Route("comment")]
    public class CommentController : BlogController
    {
        #region Private Fields

        private readonly CommentService _commentService;
        private readonly IBlogNotificationClient _notificationClient;
        private readonly IBlogConfig _blogConfig;

        #endregion

        public CommentController(
            ILogger<CommentController> logger,
            CommentService commentService,
            IBlogConfig blogConfig,
            IBlogNotificationClient notificationClient = null)
            : base(logger)
        {
            _blogConfig = blogConfig;

            _commentService = commentService;
            _notificationClient = notificationClient;
        }

        [HttpPost]
        public async Task<IActionResult> NewComment(
            PostSlugViewModelWrapper model, [FromServices] ISessionBasedCaptcha captcha)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

                if (!captcha.ValidateCaptchaCode(model.NewCommentViewModel.CaptchaCode, HttpContext.Session))
                {
                    ModelState.AddModelError(nameof(model.NewCommentViewModel.CaptchaCode), "Wrong Captcha Code");
                    return Conflict(ModelState);
                }

                var comment = model.NewCommentViewModel;
                var response = await _commentService.CreateAsync(new CommentRequest(comment.PostId)
                {
                    Username = comment.Username,
                    Content = comment.Content,
                    Email = comment.Email,
                    IpAddress = DNT ? "N/A" : HttpContext.Connection.RemoteIpAddress.ToString()
                });

                if (_blogConfig.NotificationSettings.SendEmailOnNewComment && null != _notificationClient)
                {
                    _ = Task.Run(async () =>
                    {
                        await _notificationClient.NotifyCommentAsync(response,
                            s => ContentProcessor.MarkdownToContent(s, ContentProcessor.MarkdownConvertType.Html));
                    });
                }

                if (_blogConfig.ContentSettings.RequireCommentReview)
                {
                    return Created("moonglade://empty", response);
                }

                return Ok();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error NewComment");
                return ServerError(e.Message);
            }
        }

        #region Management

        [Authorize]
        [HttpPost("set-approval-status")]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId)
        {
            await _commentService.ToggleApprovalAsync(new[] { commentId });
            return Json(commentId);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid[] commentIds)
        {
            await _commentService.DeleteAsync(commentIds);
            return Json(commentIds);
        }

        [Authorize]
        [HttpPost("reply")]
        public async Task<IActionResult> Reply(Guid commentId, string replyContent, [FromServices] LinkGenerator linkGenerator)
        {
            if (!_blogConfig.ContentSettings.EnableComments)
            {
                return Forbid();
            }

            var reply = await _commentService.AddReply(commentId, replyContent);
            if (_blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
            {
                var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);
                _ = Task.Run(async () =>
                {
                    await _notificationClient.NotifyCommentReplyAsync(reply, postLink);
                });
            }

            return Json(reply);
        }

        #endregion
    }
}