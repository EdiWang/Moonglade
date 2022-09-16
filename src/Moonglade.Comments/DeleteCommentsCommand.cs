using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record DeleteCommentsCommand(Guid[] Ids) : IRequest;

public class DeleteCommentsCommandHandler : AsyncRequestHandler<DeleteCommentsCommand>
{
    private readonly IRepository<CommentEntity> _commentRepo;
    private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

    public DeleteCommentsCommandHandler(IRepository<CommentEntity> commentRepo, IRepository<CommentReplyEntity> commentReplyRepo)
    {
        _commentRepo = commentRepo;
        _commentReplyRepo = commentReplyRepo;
    }

    protected override async Task Handle(DeleteCommentsCommand request, CancellationToken ct)
    {
        if (request.Ids is null || !request.Ids.Any())
        {
            throw new ArgumentNullException(nameof(request.Ids));
        }

        var spec = new CommentSpec(request.Ids);
        var comments = await _commentRepo.ListAsync(spec);
        foreach (var cmt in comments)
        {
            // 1. Delete all replies
            var cReplies = await _commentReplyRepo.ListAsync(new CommentReplySpec(cmt.Id));
            if (cReplies.Any())
            {
                await _commentReplyRepo.DeleteAsync(cReplies, ct);
            }

            // 2. Delete comment itself
            await _commentRepo.DeleteAsync(cmt, ct);
        }
    }
}