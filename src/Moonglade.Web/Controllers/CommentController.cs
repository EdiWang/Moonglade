using System;
using System.Net;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Notification;
using Moonglade.Web.Models;
using Newtonsoft.Json;

namespace Moonglade.Web.Controllers
{
    [Route("comment")]
    public partial class CommentController : MoongladeController
    {
        #region Private Fields

        private readonly CommentService _commentService;
        private readonly IMoongladeNotificationClient _notificationClient;
        private readonly PostService _postService;
        private readonly IBlogConfig _blogConfig;

        #endregion

        public CommentController(
            ILogger<CommentController> logger,
            IOptions<AppSettings> settings,
            CommentService commentService,
            IMoongladeNotificationClient notificationClient,
            PostService postService,
            IBlogConfig blogConfig)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;

            _commentService = commentService;
            _notificationClient = notificationClient;
            _postService = postService;
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
                        Logger.LogWarning($"Wrong Captcha Code, model: {JsonConvert.SerializeObject(model.NewCommentViewModel)}");
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
                        if (_blogConfig.EmailSettings.SendEmailOnNewComment)
                        {
                            _ = Task.Run(async () =>
                              {
                                  await _notificationClient.SendNewCommentNotificationAsync(response.Item, Utils.MdContentToHtml);
                              });
                        }
                        var cResponse = new CommentResponse(true, CommentResponseCode.Success);
                        return Json(cResponse);
                    }

                    CommentResponse failedResponse;
                    switch (response.ResponseCode)
                    {
                        case (int)ResponseFailureCode.EmailDomainBlocked:
                            Logger.LogWarning($"User email domain is blocked. model: {JsonConvert.SerializeObject(model)}");
                            Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            failedResponse = new CommentResponse(false, CommentResponseCode.EmailDomainBlocked);
                            break;
                        case (int)ResponseFailureCode.CommentDisabled:
                            Logger.LogWarning($"Comment is disabled in settings, but user somehow called NewComment() method. model: {JsonConvert.SerializeObject(model)}");
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
            Success,
            UnknownError,
            WrongCaptcha,
            EmailDomainBlocked,
            CommentDisabled,
            InvalidModel
        }
    }
}