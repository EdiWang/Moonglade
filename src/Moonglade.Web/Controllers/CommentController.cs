using System;
using System.Net;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;

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
            IOptions<AppSettings> settings,
            CommentService commentService,
            IBlogConfig blogConfig,
            IBlogNotificationClient notificationClient = null)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;

            _commentService = commentService;
            _notificationClient = notificationClient;
        }

        [HttpPost]
        public async Task<IActionResult> NewComment(PostSlugViewModelWrapper model,
            [FromServices] ISessionBasedCaptcha captcha)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new CommentResponse(false, CommentResponseCode.InvalidModel));
                }

                if (!_blogConfig.ContentSettings.EnableComments)
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Json(new CommentResponse(false, CommentResponseCode.CommentDisabled));
                }

                // Validate BasicCaptcha Code
                if (!captcha.ValidateCaptchaCode(model.NewCommentViewModel.CaptchaCode, HttpContext.Session))
                {
                    Logger.LogWarning("Wrong Captcha Code");
                    ModelState.AddModelError(nameof(model.NewCommentViewModel.CaptchaCode), "Wrong Captcha Code");

                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new CommentResponse(false, CommentResponseCode.WrongCaptcha));
                }

                var commentPostModel = model.NewCommentViewModel;
                var response = await _commentService.CreateAsync(new NewCommentRequest(commentPostModel.PostId)
                {
                    Username = commentPostModel.Username,
                    Content = commentPostModel.Content,
                    Email = commentPostModel.Email,
                    IpAddress = HttpContext.Connection.RemoteIpAddress.ToString()
                });

                if (_blogConfig.NotificationSettings.SendEmailOnNewComment && null != _notificationClient)
                {
                    _ = Task.Run(async () =>
                    {
                        await _notificationClient.NotifyCommentAsync(response, s => BlogContentProcessor.MarkdownToContent(s, BlogContentProcessor.MarkdownConvertType.Html));
                    });
                }
                var cResponse = new CommentResponse(true,
                    _blogConfig.ContentSettings.RequireCommentReview ?
                        CommentResponseCode.Success :
                        CommentResponseCode.SuccessNonReview);

                return Json(cResponse);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error NewComment");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new CommentResponse(false, CommentResponseCode.UnknownError));
            }
        }

        #region Management

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage(int page = 1)
        {
            const int pageSize = 10;
            var commentList = await _commentService.GetCommentsAsync(pageSize, page);
            var commentsAsIPagedList =
                new StaticPagedList<CommentDetailedItem>(commentList, page, pageSize, _commentService.Count());
            return View("~/Views/Admin/ManageComments.cshtml", commentsAsIPagedList);
        }

        [Authorize]
        [HttpPost("set-approval-status")]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId)
        {
            await _commentService.ToggleApprovalStatusAsync(new[] { commentId });
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