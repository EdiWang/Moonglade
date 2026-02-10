using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moonglade.Data.DTO;
using Moonglade.Email.Client;
using Moonglade.Moderation;
using Moonglade.Web.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[CommentProviderGate]
public class CommentController(
        IEventMediator eventMediator,
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        IModeratorService moderator,
        IBlogConfig blogConfig,
        ILogger<CommentController> logger) : ControllerBase
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ModelStateDictionary>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        // Early validation checks
        var validationResult = ValidateCommentRequest(request);
        if (validationResult != null) return validationResult;

        if (!blogConfig.CommentSettings.EnableComments)
        {
            return Forbid();
        }

        // Apply word filtering
        var filterResult = await ApplyWordFilteringAsync(request);
        if (filterResult != null) return filterResult;

        var ip = ClientIPHelper.GetClientIP(HttpContext);
        var item = await commandMediator.SendAsync(new CreateCommentCommand(postId, request, ip));

        if (item == null)
        {
            ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
            return ValidationProblem(ModelState);
        }

        try
        {
            await SendNewCommentNotificationAsync(item);
        }
        catch (Exception ex)
        {
            // Log the error but don't block the response
            logger.LogError(ex, "Failed to send new comment notification for post {PostId}", postId);
        }

        return Ok(new
        {
            blogConfig.CommentSettings.RequireCommentReview
        });
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        try
        {
            await commandMediator.SendAsync(new ToggleApprovalCommand([commentId]));
            return Ok(commentId);
        }
        catch (ArgumentException)
        {
            return NotFound($"Comment with ID {commentId} not found.");
        }
    }

    [HttpDelete]
    [ProducesResponseType<Guid[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        try
        {
            await commandMediator.SendAsync(new DeleteCommentsCommand(commentIds));
            return Ok(commentIds);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 5, [FromQuery] string searchTerm = null)
    {
        var comments = await queryMediator.QueryAsync(new ListCommentsQuery(pageSize, pageIndex, searchTerm));
        var count = await queryMediator.QueryAsync(new CountCommentsQuery(searchTerm));

        // Convert markdown to HTML for display
        var commentsWithHtml = comments.Select(c => new
        {
            c.Id,
            c.Username,
            c.Email,
            c.CreateTimeUtc,
            CommentContent = ContentProcessor.MarkdownToContent(c.CommentContent, ContentProcessor.MarkdownConvertType.Html),
            c.IpAddress,
            c.PostTitle,
            c.IsApproved,
            Replies = c.Replies.Select(r => new
            {
                r.ReplyTimeUtc,
                r.ReplyContent,
                ReplyContentHtml = ContentProcessor.MarkdownToContent(r.ReplyContent, ContentProcessor.MarkdownConvertType.Html)
            }).ToList()
        }).ToList();

        return Ok(new
        {
            Comments = commentsWithHtml,
            TotalRows = count,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType<CommentReply>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent)
    {
        if (string.IsNullOrWhiteSpace(replyContent))
        {
            return BadRequest("Reply content cannot be empty.");
        }

        if (!blogConfig.CommentSettings.EnableComments)
            return Forbid();

        try
        {
            var reply = await commandMediator.SendAsync(new ReplyCommentCommand(commentId, replyContent));

            // Send email notification (fire-and-forget)
            _ = Task.Run(async () => await SendReplyNotificationAsync(reply));

            return Ok(reply);
        }
        catch (ArgumentException)
        {
            return NotFound($"Comment with ID {commentId} not found.");
        }
    }

    #region Private Methods

    private IActionResult ValidateCommentRequest(CommentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !Helper.IsValidEmailAddress(request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Invalid email address.");
            return ValidationProblem(ModelState);
        }

        return null;
    }

    private async Task<IActionResult> ApplyWordFilteringAsync(CommentRequest request)
    {
        if (!blogConfig.CommentSettings.EnableWordFilter)
        {
            return null;
        }

        switch (blogConfig.CommentSettings.WordFilterMode)
        {
            case WordFilterMode.Mask:
                request.Username = await moderator.Mask(request.Username);
                request.Content = await moderator.Mask(request.Content);
                break;

            case WordFilterMode.Block:
                if (await moderator.Detect(request.Username, request.Content))
                {
                    ModelState.AddModelError(nameof(request.Content), "Your comment contains inappropriate content.");
                    return ValidationProblem(ModelState);
                }
                break;
        }

        return null;
    }

    private async Task SendNewCommentNotificationAsync(CommentDetailedItem item)
    {
        if (!blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            return;
        }

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
            logger.LogError(e, "Failed to send new comment notification for comment {CommentId}", item.Id);
        }
    }

    private async Task SendReplyNotificationAsync(CommentReply reply)
    {
        if (!blogConfig.NotificationSettings.SendEmailOnCommentReply || string.IsNullOrWhiteSpace(reply.Email))
        {
            return;
        }

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
            logger.LogError(e, "Failed to send reply notification for reply to comment {CommentId}", reply.Email);
        }
    }

    private string GetPostUrl(string routeLink)
    {
        var baseUri = new Uri(UrlHelper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
        var link = new Uri(baseUri, $"post/{routeLink.ToLower()}");
        return link.ToString();
    }

    #endregion
}