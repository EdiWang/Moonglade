using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moonglade.Comments.Moderator;
using Moonglade.Email.Client;
using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[CommentProviderGate]
public class CommentController(
        IEventMediator eventMediator,
        ICommandMediator commandMediator,
        IModeratorService moderator,
        IBlogConfig blogConfig,
        ILogger<CommentController> logger) : ControllerBase
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ModelStateDictionary>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !Helper.IsValidEmailAddress(request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Invalid Email address.");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        if (!blogConfig.CommentSettings.EnableComments) return Forbid();

        if (blogConfig.CommentSettings.EnableWordFilter)
        {
            switch (blogConfig.CommentSettings.WordFilterMode)
            {
                case WordFilterMode.Mask:
                    request.Username = await moderator.Mask(request.Username);
                    request.Content = await moderator.Mask(request.Content);
                    break;
                case WordFilterMode.Block:
                    if (await moderator.Detect(request.Username, request.Content))
                    {
                        await Task.CompletedTask;
                        ModelState.AddModelError(nameof(request.Content), "Your comment contains bad bad word.");
                        return Conflict(ModelState);
                    }
                    break;
            }
        }

        var ip = Helper.GetClientIP(HttpContext);
        var item = await commandMediator.SendAsync(new CreateCommentCommand(postId, request, ip));

        if (null == item)
        {
            ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
            return Conflict(ModelState);
        }

        if (blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            try
            {
                await eventMediator.PublishAsync(new CommentEvent(
                    item.Username,
                    item.Email,
                    item.IpAddress,
                    item.PostTitle,
                    item.CommentContent));
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

        return Ok(new
        {
            blogConfig.CommentSettings.RequireCommentReview
        });
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        await commandMediator.SendAsync(new ToggleApprovalCommand([commentId]));
        return Ok(commentId);
    }

    [HttpDelete]
    [ProducesResponseType<Guid[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        await commandMediator.SendAsync(new DeleteCommentsCommand(commentIds));
        return Ok(commentIds);
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType<CommentReply>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent)
    {
        if (!blogConfig.CommentSettings.EnableComments) return Forbid();

        var reply = await commandMediator.SendAsync(new ReplyCommentCommand(commentId, replyContent));
        if (blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
        {
            var postLink = GetPostUrl(reply.RouteLink);

            try
            {
                await eventMediator.PublishAsync(new CommentReplyEvent(
                    reply.Email,
                    reply.CommentContent,
                    reply.Title,
                    reply.ReplyContentHtml,
                    postLink));
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

        return Ok(reply);
    }

    private string GetPostUrl(string routeLink)
    {
        var baseUri = new Uri(Helper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
        var link = new Uri(baseUri, $"post/{routeLink.ToLower()}");
        return link.ToString();
    }
}