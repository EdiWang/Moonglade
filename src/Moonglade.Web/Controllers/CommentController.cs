using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;
using Moonglade.Email.Client;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[CommentProviderGate]
public class CommentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBlogConfig _blogConfig;
    private readonly ILogger<CommentController> _logger;

    public CommentController(
        IMediator mediator,
        IBlogConfig blogConfig,
        ILogger<CommentController> logger)
    {
        _mediator = mediator;
        _blogConfig = blogConfig;
        _logger = logger;
    }

    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !Helper.IsValidEmailAddress(request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Invalid Email address.");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

        var ip = (bool)HttpContext.Items["DNT"]! ? "N/A" : Helper.GetClientIP(HttpContext);
        var item = await _mediator.Send(new CreateCommentCommand(postId, request, ip));

        switch (item.Status)
        {
            case -1:
                ModelState.AddModelError(nameof(request.Content), "Your comment contains bad bad word.");
                return Conflict(ModelState);
            case -2:
                ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
                return Conflict(ModelState);
        }

        if (_blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            try
            {
                await _mediator.Publish(new CommentNotification(
                    item.Item.Username,
                    item.Item.Email,
                    item.Item.IpAddress,
                    item.Item.PostTitle,
                    item.Item.CommentContent));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        if (_blogConfig.ContentSettings.RequireCommentReview)
        {
            return Created("moonglade://empty", item);
        }

        return Ok();
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        await _mediator.Send(new ToggleApprovalCommand(new[] { commentId }));
        return Ok(commentId);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(Guid[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        await _mediator.Send(new DeleteCommentsCommand(commentIds));
        return Ok(commentIds);
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType(typeof(CommentReply), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent,
        LinkGenerator linkGenerator)
    {
        if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

        var reply = await _mediator.Send(new ReplyCommentCommand(commentId, replyContent));
        if (_blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
        {
            var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);

            try
            {
                await _mediator.Publish(new CommentReplyNotification(
                    reply.Email,
                    reply.CommentContent,
                    reply.Title,
                    reply.ReplyContentHtml,
                    postLink));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
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