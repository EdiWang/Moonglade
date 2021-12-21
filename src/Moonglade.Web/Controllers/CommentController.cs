using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moonglade.Notification.Client;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[CommentProviderGate]
public class CommentController : ControllerBase
{
    #region Private Fields

    private readonly IMediator _mediator;

    private readonly ITimeZoneResolver _timeZoneResolver;
    private readonly IBlogConfig _blogConfig;

    #endregion

    public CommentController(
        IMediator mediator,
        IBlogConfig blogConfig,
        ITimeZoneResolver timeZoneResolver)
    {
        _mediator = mediator;
        _blogConfig = blogConfig;
        _timeZoneResolver = timeZoneResolver;
    }

    [HttpGet("list/{postId:guid}")]
    [FeatureGate(FeatureFlags.EnableWebApi)]
    [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([NotEmpty] Guid postId)
    {
        var comments = await _mediator.Send(new GetApprovedCommentsQuery(postId));
        var resp = comments.Select(p => new
        {
            p.Username,
            Content = p.CommentContent,
            p.CreateTimeUtc,
            CreateTimeLocal = _timeZoneResolver.ToTimeZone(p.CreateTimeUtc),
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
    [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request, [FromServices] IServiceScopeFactory factory)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !Helper.IsValidEmailAddress(request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Invalid Email address.");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

        var ip = (bool)HttpContext.Items["DNT"] ? "N/A" : HttpContext.Connection.RemoteIpAddress?.ToString();
        var item = await _mediator.Send(new CreateCommentCommand(postId, request, ip));

        if (item is null)
        {
            ModelState.AddModelError(nameof(request.Content), "Your comment contains bad bad word.");
            return Conflict(ModelState);
        }

        if (_blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            _ = Task.Run(async () =>
            {
                var scope = factory.CreateScope();
                var mediator = scope.ServiceProvider.GetService<IMediator>();
                if (mediator != null)
                {
                    await mediator.Publish(new CommentNotification(
                        item.Username,
                        item.Email,
                        item.IpAddress,
                        item.PostTitle,
                        item.CommentContent,
                        item.CreateTimeUtc));
                }
            });
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
    public async Task<IActionResult> Delete([FromBody] Guid[] commentIds)
    {
        if (commentIds.Length == 0)
        {
            ModelState.AddModelError(nameof(commentIds), "value is empty");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        await _mediator.Send(new DeleteCommentsCommand(commentIds));
        return Ok(commentIds);
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType(typeof(CommentReply), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent,
        [FromServices] LinkGenerator linkGenerator,
        [FromServices] IServiceScopeFactory factory)
    {
        if (!_blogConfig.ContentSettings.EnableComments) return Forbid();

        var reply = await _mediator.Send(new ReplyCommentCommand(commentId, replyContent));
        if (_blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
        {
            var postLink = GetPostUrl(linkGenerator, reply.PubDateUtc, reply.Slug);
            _ = Task.Run(async () =>
            {
                var scope = factory.CreateScope();
                var mediator = scope.ServiceProvider.GetService<IMediator>();
                if (mediator != null)
                {
                    await mediator.Publish(new CommentReplyNotification(
                        reply.Email,
                        reply.CommentContent,
                        reply.Title,
                        reply.ReplyContentHtml,
                        postLink));
                }
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