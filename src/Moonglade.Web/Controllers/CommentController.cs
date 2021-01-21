using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moonglade.Comments;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core.Notification;
using Moonglade.Utils;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        #region Private Fields

        private readonly ICommentService _commentService;
        private readonly IBlogNotificationClient _notificationClient;
        private readonly IBlogConfig _blogConfig;

        #endregion

        public CommentController(
            ICommentService commentService,
            IBlogConfig blogConfig,
            IBlogNotificationClient notificationClient = null)
        {
            _blogConfig = blogConfig;

            _commentService = commentService;
            _notificationClient = notificationClient;
        }

        [HttpPost("{postId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> NewComment(Guid postId, NewCommentModel model, [FromServices] ISessionBasedCaptcha captcha)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!string.IsNullOrWhiteSpace(model.Email) && !Helper.IsValidEmailAddress(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Invalid Email address.");
                return BadRequest(ModelState);
            }

            if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

            if (!captcha.ValidateCaptchaCode(model.CaptchaCode, HttpContext.Session))
            {
                ModelState.AddModelError(nameof(model.CaptchaCode), "Wrong Captcha Code");
                return Conflict(ModelState);
            }

            var response = await _commentService.CreateAsync(new(postId)
            {
                Username = model.Username,
                Content = model.Content,
                Email = model.Email,
                IpAddress = (bool)HttpContext.Items["DNT"] ? "N/A" : HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            if (response is null)
            {
                ModelState.AddModelError(nameof(model.Content), "Your comment contains bad bad word.");
                return Conflict(ModelState);
            }

            if (_blogConfig.NotificationSettings.SendEmailOnNewComment && _notificationClient is not null)
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

        [HttpPost("set-approval-status/{commentId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetApprovalStatus(Guid commentId)
        {
            if (commentId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(commentId), "value is empty");
                return BadRequest(ModelState);
            }

            await _commentService.ToggleApprovalAsync(new[] { commentId });
            return Ok(commentId);
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete([FromBody] Guid[] commentIds)
        {
            if (commentIds.Length == 0)
            {
                ModelState.AddModelError(nameof(commentIds), "value is empty");
                return BadRequest(ModelState);
            }

            await _commentService.DeleteAsync(commentIds);
            return Ok(commentIds);
        }

        [HttpPost("reply")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Reply(ReplyRequest request, [FromServices] LinkGenerator linkGenerator)
        {
            if (request.CommentId == Guid.Empty) ModelState.AddModelError(nameof(request.CommentId), "value is empty");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

            var reply = await _commentService.AddReply(request.CommentId, request.ReplyContent);
            if (_blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
            {
                var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);
                _ = Task.Run(async () =>
                {
                    await _notificationClient.NotifyCommentReplyAsync(reply, postLink);
                });
            }

            return Ok(reply);
        }

        private string GetPostUrl(LinkGenerator linkGenerator, DateTime pubDate, string slug)
        {
            var link = linkGenerator.GetUriByAction(HttpContext, "Slug", "Post",
                new
                {
                    year = pubDate.Year,
                    month = pubDate.Month,
                    day = pubDate.Day,
                    slug
                });
            return link;
        }
    }

    public class ReplyRequest
    {
        public Guid CommentId { get; set; }

        [Required]
        public string ReplyContent { get; set; }
    }
}