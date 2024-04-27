using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record DeleteCommentsCommand(Guid[] Ids) : IRequest;

public class DeleteCommentsCommandHandler(MoongladeRepository<CommentEntity> commentRepo, MoongladeRepository<CommentReplyEntity> commentReplyRepo) : IRequestHandler<DeleteCommentsCommand>
{
    public async Task Handle(DeleteCommentsCommand request, CancellationToken ct)
    {
        var spec = new CommentByIdsSepc(request.Ids);
        var comments = await commentRepo.ListAsync(spec, ct);
        foreach (var cmt in comments)
        {
            cmt.Replies.Clear();
            await commentRepo.DeleteAsync(cmt, ct);
        }
    }
}