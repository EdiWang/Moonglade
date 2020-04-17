using System;
using System.Net;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public partial class CommentController : MoongladeController
    {
        #region Private Fields

        private readonly CommentService _commentService;
        private readonly IMoongladeNotificationClient _notificationClient;
        private readonly IBlogConfig _blogConfig;

        #endregion

        public CommentController(
            ILogger<CommentController> logger,
            IOptions<AppSettings> settings,
            CommentService commentService,
            IBlogConfig blogConfig,
            IMoongladeNotificationClient notificationClient = null)
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
                if (ModelState.IsValid)
                {
                    // Validate BasicCaptcha Code
                    if (!captcha.ValidateCaptchaCode(model.NewCommentViewModel.CaptchaCode, HttpContext.Session))
                    {
                        Logger.LogWarning("Wrong Captcha Code");
                        ModelState.AddModelError(nameof(model.NewCommentViewModel.CaptchaCode), "Wrong Captcha Code");

                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        var cResponse = new CommentResponse(false, CommentResponseCode.WrongCaptcha);
                        return Json(cResponse);
                    }

                    var commentPostModel = model.NewCommentViewModel;
                    var response = await _commentService.AddCommentAsync(new NewCommentRequest(commentPostModel.PostId)
                    {
                        Username = commentPostModel.Username,
                        Content = commentPostModel.Content,
                        Email = commentPostModel.Email,
                        IpAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        UserAgent = GetUserAgent()
                    });

                    if (response.IsSuccess)
                    {
                        if (_blogConfig.EmailSettings.SendEmailOnNewComment && null != _notificationClient)
                        {
                            _ = Task.Run(async () =>
                              {
                                  await _notificationClient.SendNewCommentNotificationAsync(response.Item, s => Utils.ConvertMarkdownContent(s, Utils.MarkdownConvertType.Html));
                              });
                        }
                        var cResponse = new CommentResponse(true,
                            _blogConfig.ContentSettings.RequireCommentReview ?
                            CommentResponseCode.Success :
                            CommentResponseCode.SuccessNonReview);

                        return Json(cResponse);
                    }

                    CommentResponse failedResponse;
                    switch (response.ResponseCode)
                    {
                        case (int)ResponseFailureCode.EmailDomainBlocked:
                            Logger.LogWarning("User email domain is blocked.");
                            Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            failedResponse = new CommentResponse(false, CommentResponseCode.EmailDomainBlocked);
                            break;
                        case (int)ResponseFailureCode.CommentDisabled:
                            Logger.LogWarning("Comment is disabled in settings, but user somehow called NewComment() method.");
                            Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            failedResponse = new CommentResponse(false, CommentResponseCode.CommentDisabled);
                            break;
                        default:
                            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            failedResponse = new CommentResponse(false, CommentResponseCode.UnknownError);
                            break;
                    }
                    return Json(failedResponse);
                }

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new CommentResponse(false, CommentResponseCode.InvalidModel));
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
            var commentList = await _commentService.GetPagedCommentAsync(pageSize, page);
            var commentsAsIPagedList =
                new StaticPagedList<CommentListItem>(commentList.Item, page, pageSize, _commentService.CountComments());
            return View(commentsAsIPagedList);
        }

        [Authorize]
        [HttpPost("set-approval-status")]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId)
        {
            var response = await _commentService.ToggleApprovalStatusAsync(new[] { commentId });
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
        public async Task<IActionResult> Delete(Guid commentId)
        {
            var response = await _commentService.DeleteCommentsAsync(new[] { commentId });
            Logger.LogInformation($"User '{User.Identity.Name}' deleting comment id '{commentId}'");

            return response.IsSuccess ? Json(commentId) : Json(false);
        }

        [Authorize]
        [HttpPost("reply")]
        public async Task<IActionResult> Reply(Guid commentId, string replyContent, [FromServices] LinkGenerator linkGenerator)
        {
            var response = await _commentService.AddReply(
                commentId,
                replyContent,
                HttpContext.Connection.RemoteIpAddress.ToString(),
                GetUserAgent());

            if (!response.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(response);
            }

            if (_blogConfig.EmailSettings.SendEmailOnCommentReply)
            {
                var postLink = GetPostUrl(linkGenerator, response.Item.PubDateUtc, response.Item.Slug);
                _ = Task.Run(async () =>
                {
                    await _notificationClient.SendCommentReplyNotificationAsync(response.Item, postLink);
                });
            }

            return Json(response.Item);
        }

        #endregion
    }
}