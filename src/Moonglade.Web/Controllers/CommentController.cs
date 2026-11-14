using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.RateLimiting;
using Moonglade.ActivityLog;
using Moonglade.Data.DTO;
using Moonglade.Email;
using Moonglade.Moderation;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class CommentController(
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        IModeratorService moderator,
        IBlogConfig blogConfig,
        CommentSubmissionGuard submissionGuard,
        IEventMediator eventMediator,
        ILogger<CommentController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting(CommentRateLimitPolicy.PolicyName)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        var settings = blogConfig.Capture();
        if (!settings.CommentSettings.EnableComments)
        {
            return Forbid();
        }

        // Early validation checks
        var validationResult = ValidateCommentRequest(request);
        if (validationResult != null) return validationResult;

        var submissionGuardResult = submissionGuard.Validate(request);
        if (!submissionGuardResult.Succeeded)
        {
            ModelState.AddModelError(submissionGuardResult.ModelStateKey, submissionGuardResult.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        // Apply word filtering
        var filterResult = ApplyWordFiltering(request, settings.CommentSettings);
        if (filterResult != null) return filterResult;

        var ip = ClientIPHelper.GetClientIP(HttpContext);
        var item = await CommandMediator.SendAsync(new CreateCommentCommand(postId, request, ip) { Snapshot = settings });

        if (item == null)
        {
            ModelState.AddModelError(nameof(postId), "Comment is closed for this post.");
            return ValidationProblem(ModelState);
        }

        // Send email notification
        if (settings.NotificationSettings.SendEmailOnNewComment)
        {
            try
            {
                await eventMediator.PublishAsync(
                    new CommentEvent(
                        item.Username,
                        item.Email,
                        item.IpAddress,
                        item.PostTitle,
                        item.CommentContent)
                    { Snapshot = settings },
                    cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue new comment email notification for comment {CommentId}.", item.Id);
            }
        }

        return Ok(new CommentCreateResponse(
            settings.CommentSettings.RequireCommentReview,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        try
        {
            await CommandMediator.SendAsync(new ToggleApprovalCommand([commentId]));

            await LogActivityAsync(
                ActivityEvents.CommentApprovalToggled,
                $"Comment #{commentId}",
                ActivityLogDetails.Create(("CommentId", commentId)),
                targetId: commentId.ToString());

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
                ActivityEvents.CommentDeleted,
                $"{commentIds.Length} comment(s)",
                ActivityLogDetails.Create(("CommentIds", commentIds)));

            return Ok(commentIds);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] CommentFilter filter = null)
    {
        var settings = blogConfig.Capture();
        filter ??= new();
        var comments = await queryMediator.QueryAsync(new ListCommentsQuery(pageSize, pageIndex, filter));
        var count = await queryMediator.QueryAsync(new CountCommentsQuery(filter));

        // Convert markdown to HTML for display
        var enableLinks = settings.CommentSettings.EnableMarkdownLinks;
        var commentsWithHtml = comments.Select(c => new CommentListItem(
            c.Id,
            c.Username,
            c.Email,
            c.CreateTimeUtc,
            ContentProcessor.MarkdownToCommentHtml(c.CommentContent, enableLinks),
            c.IpAddress,
            c.PostTitle,
            c.IsApproved,
            c.Replies.Select(r => new CommentReplyListItem(
                r.ReplyTimeUtc,
                r.ReplyContent,
                ContentProcessor.MarkdownToCommentHtml(r.ReplyContent, enableLinks))).ToList())).ToList();

        return Ok(new PagedResult<CommentListItem>(commentsWithHtml, pageIndex, pageSize, count));
    }

    [HttpPost("{commentId:guid}/reply")]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent)
    {
        var settings = blogConfig.Capture();
        if (string.IsNullOrWhiteSpace(replyContent))
        {
            return BadRequest("Reply content cannot be empty.");
        }

        if (!settings.CommentSettings.EnableComments)
            return Forbid();

        try
        {
            var reply = await CommandMediator.SendAsync(new ReplyCommentCommand(commentId, replyContent) { Snapshot = settings });

            await LogActivityAsync(
                ActivityEvents.CommentReplied,
                reply.Title,
                ActivityLogDetails.Create(("CommentId", commentId)),
                targetId: commentId.ToString());

            // Send email notification
            if (settings.NotificationSettings.SendEmailOnCommentReply && !string.IsNullOrWhiteSpace(reply.Email))
            {
                var postLink = UrlHelper.GetPostUrl(HttpContext, reply.RouteLink);
                try
                {
                    await eventMediator.PublishAsync(
                        new CommentReplyEvent(
                            reply.Email,
                            reply.CommentContent,
                            reply.Title,
                            reply.ReplyContentHtml,
                            postLink)
                        { Snapshot = settings },
                        cancellationToken: HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to enqueue comment reply email notification for comment {CommentId}.", commentId);
                }
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

    private IActionResult ApplyWordFiltering(CommentRequest request, CommentSettings commentSettings)
    {
        if (!commentSettings.EnableWordFilter || commentSettings.RequireCommentReview)
        {
            return null;
        }

        switch (commentSettings.WordFilterMode)
        {
            case WordFilterMode.Mask:
                request.Username = moderator.Mask(request.Username, commentSettings.WordFilterKeywords);
                request.Content = moderator.Mask(request.Content, commentSettings.WordFilterKeywords);
                break;

            case WordFilterMode.Block:
                if (moderator.Detect([request.Username, request.Content], commentSettings.WordFilterKeywords))
                {
                    ModelState.AddModelError(nameof(request.Content), "Your comment contains inappropriate content.");
                    return ValidationProblem(ModelState);
                }
                break;
        }

        return null;
    }

    #endregion
}

file sealed record CommentCreateResponse(bool RequireCommentReview, long FormRenderedUtc);

file sealed record CommentListItem(
    Guid Id,
    string Username,
    string Email,
    DateTime CreateTimeUtc,
    string CommentContent,
    string IpAddress,
    string PostTitle,
    bool IsApproved,
    IReadOnlyList<CommentReplyListItem> Replies);

file sealed record CommentReplyListItem(
    DateTime ReplyTimeUtc,
    string ReplyContent,
    string ReplyContentHtml);
