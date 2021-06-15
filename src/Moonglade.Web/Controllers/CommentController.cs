using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Notification.Client;
using Moonglade.Utils;
using Moonglade.Web.Filters;
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

        [HttpGet("list/{postId:guid}")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List([NotEmpty] Guid postId, [FromServices] ITimeZoneResolver timeZoneResolver)
        {
            var comments = await _commentService.GetApprovedCommentsAsync(postId);
            var resp = comments.Select(p => new
            {
                p.Username,
                Content = p.CommentContent,
                p.CreateTimeUtc,
                CreateTimeLocal = timeZoneResolver.ToTimeZone(p.CreateTimeUtc),
                Replies = p.CommentReplies
            });

            return Ok(resp);
        }

        [HttpPost("{postId:guid}")]
        [AllowAnonymous]
        [ServiceFilter(typeof(ValidateCaptcha))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(Guid postId, NewCommentModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Email) && !Helper.IsValidEmailAddress(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Invalid Email address.");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

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
                    await _notificationClient.NotifyCommentAsync(
                        response.Username,
                        response.Email,
                        response.IpAddress,
                        response.PostTitle,
                        response.CommentContent,
                        response.CreateTimeUtc);
                });
            }

            if (_blogConfig.ContentSettings.RequireCommentReview)
            {
                return Created("moonglade://empty", response);
            }

            return Ok();
        }

        [HttpPut("{commentId:guid}/approval/toggle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
        {
            await _commentService.ToggleApprovalAsync(new[] { commentId });
            return Ok(commentId);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(Guid[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete([FromBody] Guid[] commentIds)
        {
            if (commentIds.Length == 0)
            {
                ModelState.AddModelError(nameof(commentIds), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _commentService.DeleteAsync(commentIds);
            return Ok(commentIds);
        }

        [HttpPost("{commentId:guid}/reply")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Reply([NotEmpty] Guid commentId, [Required][FromBody] string replyContent, [FromServices] LinkGenerator linkGenerator)
        {
            if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

            var reply = await _commentService.AddReply(commentId, replyContent);
            if (_blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
            {
                var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);
                _ = Task.Run(async () =>
                {
                    await _notificationClient.NotifyCommentReplyAsync(reply.Email,
                        reply.CommentContent,
                        reply.Title,
                        reply.ReplyContentHtml,
                        postLink);
                });
            }

            return Ok(reply);
        }

        private string GetPostUrl(LinkGenerator linkGenerator, DateTime pubDate, string slug)
        {
            var link = linkGenerator.GetUriByPage(HttpContext, "/Post", null,
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
}