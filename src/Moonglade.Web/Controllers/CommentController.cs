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
        IMediator mediator,
        IModeratorService moderator,
        IBlogConfig blogConfig,
        ILogger<CommentController> logger) : ControllerBase
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
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

        if (!blogConfig.ContentSettings.EnableComments) return Forbid();

        if (blogConfig.ContentSettings.EnableWordFilter)
        {
            switch (blogConfig.ContentSettings.WordFilterMode)
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

        var ip = (bool)HttpContext.Items["DNT"]! ? "N/A" : Helper.GetClientIP(HttpContext);
        var item = await mediator.Send(new CreateCommentCommand(postId, request, ip));

        if (null == item)
        {
            ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
            return Conflict(ModelState);
        }

        if (blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            try
            {
                await mediator.Publish(new CommentNotification(
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

        return NoContent();
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        await mediator.Send(new ToggleApprovalCommand(new[] { commentId }));
        return Ok(commentId);
    }

    [HttpDelete]
    [ProducesResponseType<Guid[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        await mediator.Send(new DeleteCommentsCommand(commentIds));
        return Ok(commentIds);
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType<CommentReply>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent,
        LinkGenerator linkGenerator)
    {
        if (!blogConfig.ContentSettings.EnableComments) return Forbid();

        var reply = await mediator.Send(new ReplyCommentCommand(commentId, replyContent));
        if (blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
        {
            var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);

            try
            {
                await mediator.Publish(new CommentReplyNotification(
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