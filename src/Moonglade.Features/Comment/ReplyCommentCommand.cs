using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Comment;

public record ReplyCommentCommand(Guid CommentId, string ReplyContent) : ICommand<CommentReply>;

public class ReplyCommentCommandHandler(
    ILogger<ReplyCommentCommandHandler> logger,
    BlogDbContext db) : ICommandHandler<ReplyCommentCommand, CommentReply>
{
    public async Task<CommentReply> HandleAsync(ReplyCommentCommand request, CancellationToken ct)
    {
        var cmt = await db.Comment
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            ?? throw new InvalidOperationException($"Comment {request.CommentId} is not found.");

        var id = Guid.NewGuid();
        var model = new CommentReplyEntity
        {
            Id = id,
            ReplyContent = request.ReplyContent,
            CreateTimeUtc = DateTime.UtcNow,
            CommentId = request.CommentId
        };

        await db.CommentReply.AddAsync(model, ct);
        await db.SaveChangesAsync(ct);

        var reply = new CommentReply
        {
            CommentContent = cmt.CommentContent,
            CommentId = request.CommentId,
            Email = cmt.Email,
            Id = model.Id,
            PostId = cmt.PostId,
            ReplyContent = model.ReplyContent,
            ReplyContentHtml = ContentProcessor.MarkdownToContent(model.ReplyContent, ContentProcessor.MarkdownConvertType.Html),
            ReplyTimeUtc = model.CreateTimeUtc,
            RouteLink = cmt.Post.RouteLink,
            Title = cmt.Post.Title
        };

        logger.LogInformation("Replied comment '{CommentId}' with reply '{ReplyId}'", request.CommentId, id);
        return reply;
    }
}