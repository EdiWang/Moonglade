using System;
using System.Net;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using Newtonsoft.Json;

namespace Moonglade.Web.Controllers
{
    [Route("comment")]
    public partial class CommentController : MoongladeController
    {
        #region Private Fields

        private readonly CommentService _commentService;
        private readonly EmailService _emailService;
        private readonly PostService _postService;
        private readonly ISessionBasedCaptcha _captcha;
        private readonly BlogConfig _blogConfig;
        private readonly LinkGenerator _linkGenerator;

        #endregion

        public CommentController(
            ILogger<CommentController> logger,
            IOptions<AppSettings> settings,
            IMemoryCache memoryCache,
            CommentService commentService,
            EmailService emailService,
            PostService postService,
            ISessionBasedCaptcha captcha,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            LinkGenerator linkGenerator)
            : base(logger, settings, memoryCache: memoryCache)
        {
            _blogConfig = blogConfig;
            _linkGenerator = linkGenerator;
            _blogConfig.Initialize(blogConfigurationService);

            _commentService = commentService;
            _emailService = emailService;
            _postService = postService;
            _captcha = captcha;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewComment(PostSlugViewModelWrapper model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validate BasicCaptcha Code
                    if (!_captcha.ValidateCaptchaCode(model.NewCommentModel.CaptchaCode, HttpContext.Session))
                    {
                        Logger.LogWarning($"Wrong Captcha Code, model: {JsonConvert.SerializeObject(model.NewCommentModel)}");
                        ModelState.AddModelError(nameof(model.NewCommentModel.CaptchaCode), "Wrong Captcha Code");

                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        var cResponse = new CommentResponse(false, CommentResponseCode.WrongCaptcha);
                        return Json(cResponse);
                    }

                    var commentPostModel = model.NewCommentModel;
                    var response = _commentService.NewComment(commentPostModel.Username, commentPostModel.Content,
                                                              commentPostModel.PostId, commentPostModel.Email,
                                                              HttpContext.Connection.RemoteIpAddress.ToString(),
                                                              GetUserAgent());

                    if (response.IsSuccess)
                    {
                        if (_blogConfig.EmailConfiguration.SendEmailOnNewComment)
                        {
                            var postTitle = _postService.GetPostTitle(commentPostModel.PostId);
                            Task.Run(async () =>
                            {
                                await _emailService.SendNewCommentNotificationAsync(response.Item, postTitle);
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
    }
}