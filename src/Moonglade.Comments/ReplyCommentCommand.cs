using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Comments;

public record ReplyCommentCommand(Guid CommentId, string ReplyContent) : ICommand<CommentReply>;

public class ReplyCommentCommandHandler(
    ILogger<ReplyCommentCommandHandler> logger,
    MoongladeRepository<CommentEntity> commentRepo,
    MoongladeRepository<CommentReplyEntity> commentReplyRepo) : ICommandHandler<ReplyCommentCommand, CommentReply>
{
    public async Task<CommentReply> HandleAsync(ReplyCommentCommand request, CancellationToken ct)
    {
        var cmt = await commentRepo.FirstOrDefaultAsync(new CommentWithPostByIdSpec(request.CommentId), ct)
            ?? throw new InvalidOperationException($"Comment {request.CommentId} is not found.");

        var id = Guid.NewGuid();
        var model = new CommentReplyEntity
        {
            Id = id,
            ReplyContent = request.ReplyContent,
            CreateTimeUtc = DateTime.UtcNow,
            CommentId = request.CommentId
        };

        await commentReplyRepo.AddAsync(model, ct);

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