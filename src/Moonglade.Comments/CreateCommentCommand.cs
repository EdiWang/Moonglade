using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record CreateCommentCommand(
    Guid PostId,
    CommentRequest Payload,
    string IpAddress
) : IRequest<CommentDetailedItem>;

public class CreateCommentCommandHandler(
    IBlogConfig blogConfig,
    ILogger<CreateCommentCommandHandler> logger,
    MoongladeRepository<PostEntity> postRepository,
    MoongladeRepository<CommentEntity> commentRepository
) : IRequestHandler<CreateCommentCommand, CommentDetailedItem>
{
    public async Task<CommentDetailedItem> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        // Validate input
        if (request.Payload == null)
        {
            logger.LogWarning("Comment payload is null.");
            return null;
        }

        // Fetch post info
        var spec = new PostByIdForTitleDateSpec(request.PostId);
        var postInfo = await postRepository.FirstOrDefaultAsync(spec, ct);

        // Check if comments are closed
        if (blogConfig.CommentSettings.CloseCommentAfterDays > 0)
        {
            var daysSincePublished = (DateTime.UtcNow.Date - postInfo.PubDateUtc.GetValueOrDefault()).Days;
            if (daysSincePublished > blogConfig.CommentSettings.CloseCommentAfterDays)
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
            IsApproved = !blogConfig.CommentSettings.RequireCommentReview
        };

        await commentRepository.AddAsync(comment, ct);

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
