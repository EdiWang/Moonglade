using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

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
                            Logger.LogWarning($"User email domain is blocked. model: {JsonSerializer.Serialize(model)}");
                            Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            failedResponse = new CommentResponse(false, CommentResponseCode.EmailDomainBlocked);
                            break;
                        case (int)ResponseFailureCode.CommentDisabled:
                            Logger.LogWarning($"Comment is disabled in settings, but user somehow called NewComment() method. model: {JsonSerializer.Serialize(model)}");
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

        public class CommentResponse
        {
            public bool IsSuccess { get; set; }

            public CommentResponseCode ResponseCode { get; set; }

            public CommentResponse(bool isSuccess, CommentResponseCode responseCode)
            {
                IsSuccess = isSuccess;
                ResponseCode = responseCode;
            }
        }

        public enum CommentResponseCode
        {
            Success = 100,
            SuccessNonReview = 101,
            UnknownError = 200,
            WrongCaptcha = 300,
            EmailDomainBlocked = 400,
            CommentDisabled = 500,
            InvalidModel = 600
        }
    }
}