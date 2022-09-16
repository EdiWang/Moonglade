using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Comments;

public record ReplyCommentCommand(Guid CommentId, string ReplyContent) : IRequest<CommentReply>;

public class ReplyCommentCommandHandler : IRequestHandler<ReplyCommentCommand, CommentReply>
{
    private readonly IRepository<CommentEntity> _commentRepo;
    private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

    public ReplyCommentCommandHandler(IRepository<CommentEntity> commentRepo, IRepository<CommentReplyEntity> commentReplyRepo)
    {
        _commentRepo = commentRepo;
        _commentReplyRepo = commentReplyRepo;
    }

    public async Task<CommentReply> Handle(ReplyCommentCommand request, CancellationToken ct)
    {
        var cmt = await _commentRepo.GetAsync(request.CommentId, ct);
        if (cmt is null) throw new InvalidOperationException($"Comment {request.CommentId} is not found.");

        var id = Guid.NewGuid();
        var model = new CommentReplyEntity
        {
            Id = id,
            ReplyContent = request.ReplyContent,
            CreateTimeUtc = DateTime.UtcNow,
            CommentId = request.CommentId
        };

        await _commentReplyRepo.AddAsync(model, ct);

        var reply = new CommentReply
        {
            CommentContent = cmt.CommentContent,
            CommentId = request.CommentId,
            Email = cmt.Email,
            Id = model.Id,
            PostId = cmt.PostId,
            PubDateUtc = cmt.Post.PubDateUtc.GetValueOrDefault(),
            ReplyContent = model.ReplyContent,
            ReplyContentHtml = ContentProcessor.MarkdownToContent(model.ReplyContent, ContentProcessor.MarkdownConvertType.Html),
            ReplyTimeUtc = model.CreateTimeUtc,
            Slug = cmt.Post.Slug,
            Title = cmt.Post.Title
        };

        return reply;
    }
}