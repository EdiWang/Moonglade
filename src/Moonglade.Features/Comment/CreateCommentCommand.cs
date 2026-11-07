using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.DTO;
using System.Text.Json.Serialization;

namespace Moonglade.Features.Comment;

public record CreateCommentCommand(
    Guid PostId,
    CommentRequest Payload,
    string IpAddress
) : ICommand<CommentDetailedItem>
{
    [JsonIgnore]
    public BlogSettingsSnapshot Snapshot { get; init; }
}

public class CreateCommentCommandHandler(
    IBlogConfig blogConfig,
    ILogger<CreateCommentCommandHandler> logger,
    BlogDbContext db
) : ICommandHandler<CreateCommentCommand, CommentDetailedItem>
{
    public async Task<CommentDetailedItem> HandleAsync(CreateCommentCommand request, CancellationToken ct)
    {
        var settings = request.Snapshot ?? blogConfig.Capture();
        // Validate input
        if (request.Payload == null)
        {
            logger.LogWarning("Comment payload is null.");
            return null;
        }

        // Fetch post info
        var postInfo = await db.Post
            .Where(p => p.Id == request.PostId)
            .Select(p => new CommentPostInfo(p.Title, p.PubDateUtc))
            .FirstOrDefaultAsync(ct);

        if (postInfo is null)
        {
            logger.LogWarning("Comment target post {PostId} was not found.", request.PostId);
            return null;
        }

        // Check if comments are closed
        if (settings.CommentSettings.CloseCommentAfterDays > 0)
        {
            var daysSincePublished = (DateTime.UtcNow.Date - postInfo.PubDateUtc.GetValueOrDefault()).Days;
            if (daysSincePublished > settings.CommentSettings.CloseCommentAfterDays)
            {
                logger.LogInformation("Comments are closed for post {PostId} after {Days} days.", request.PostId, daysSincePublished);
                return null;
            }
        }

        // Create comment entity
        var comment = new CommentEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Payload.Username,
            CommentContent = request.Payload.Content,
            PostId = request.PostId,
            CreateTimeUtc = DateTime.UtcNow,
            Email = request.Payload.Email,
            IPAddress = request.IpAddress,
            IsApproved = !settings.CommentSettings.RequireCommentReview
        };

        await db.Comment.AddAsync(comment, ct);
        await db.SaveChangesAsync(ct);

        var result = new CommentDetailedItem
        {
            Id = comment.Id,
            CommentContent = comment.CommentContent,
            CreateTimeUtc = comment.CreateTimeUtc,
            Email = comment.Email,
            IpAddress = comment.IPAddress,
            IsApproved = comment.IsApproved,
            PostTitle = postInfo.Title,
            Username = comment.Username
        };

        logger.LogInformation("New comment created: {CommentId}", result.Id);
        return result;
    }
}

file sealed record CommentPostInfo(string Title, DateTime? PubDateUtc);
