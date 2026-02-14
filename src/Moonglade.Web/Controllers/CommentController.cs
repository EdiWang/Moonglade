using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Email.Client;
using Moonglade.Moderation;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class CommentController(
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        IModeratorService moderator,
        IBlogConfig blogConfig,
        CannonService cannonService) : BlogControllerBase(commandMediator)
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        if (!blogConfig.CommentSettings.EnableComments)
        {
            return Forbid();
        }

        // Early validation checks
        var validationResult = ValidateCommentRequest(request);
        if (validationResult != null) return validationResult;

        // Apply word filtering
        var filterResult = await ApplyWordFilteringAsync(request);
        if (filterResult != null) return filterResult;

        var ip = ClientIPHelper.GetClientIP(HttpContext);
        var item = await CommandMediator.SendAsync(new CreateCommentCommand(postId, request, ip));

        if (item == null)
        {
            ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
            return ValidationProblem(ModelState);
        }

        await LogActivityAsync(
            EventType.CommentCreated,
            "Create Comment",
            item.PostTitle,
            new { CommentId = item.Id, item.Username, PostId = postId });

        // Send email notification (fire-and-forget)
        if (blogConfig.NotificationSettings.SendEmailOnNewComment)
        {
            cannonService.FireAsync<IEventMediator>(async mediator =>
                await mediator.PublishAsync(new CommentEvent(
                    item.Username,
                    item.Email,
                    item.IpAddress,
                    item.PostTitle,
                    item.CommentContent)));
        }

        return Ok(new
        {
            blogConfig.CommentSettings.RequireCommentReview
        });
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        try
        {
            await CommandMediator.SendAsync(new ToggleApprovalCommand([commentId]));

            await LogActivityAsync(
                EventType.CommentApprovalToggled,
                "Toggle Comment Approval",
                $"Comment #{commentId}",
                new { CommentId = commentId });

            return Ok(commentId);
        }
        catch (ArgumentException)
        {
            return NotFound($"Comment with ID {commentId} not found.");
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        try
        {
            await CommandMediator.SendAsync(new DeleteCommentsCommand(commentIds));

            await LogActivityAsync(
                EventType.CommentDeleted,
                "Delete Comments",
                $"{commentIds.Length} comment(s)",
                new { CommentIds = commentIds });

            return Ok(commentIds);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("list")]
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
            var reply = await CommandMediator.SendAsync(new ReplyCommentCommand(commentId, replyContent));

            await LogActivityAsync(
                EventType.CommentReplied,
                "Reply to Comment",
                reply.Title,
                new { CommentId = commentId, ReplyContent = replyContent });

            // Send email notification (fire-and-forget)
            if (blogConfig.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
            {
                var postLink = GetPostUrl(reply.RouteLink);
                cannonService.FireAsync<IEventMediator>(async mediator =>
                    await mediator.PublishAsync(new CommentReplyEvent(
                        reply.Email,
                        reply.CommentContent,
                        reply.Title,
                        reply.ReplyContentHtml,
                        postLink)));
            }

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

    private string GetPostUrl(string routeLink)
    {
        var baseUri = new Uri(UrlHelper.ResolveRootUrl(HttpContext, null, removeTailSlash: true));
        var link = new Uri(baseUri, $"post/{routeLink.ToLower()}");
        return link.ToString();
    }

    #endregion
}